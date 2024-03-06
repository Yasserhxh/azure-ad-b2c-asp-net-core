using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TestApp.Models;
using TestApp.Proxy;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.PronunciationAssessment;
using System.Drawing;
using System.Net.Http;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http;
using Azure.Core;
using System.Net;
using Microsoft.CognitiveServices.Speech.Speaker;
using System.Collections.Generic;

namespace TestApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly TestServiceProxy testService;
        private static readonly HttpClient _httpClient = new();
        private readonly string subscriptionKey = "ebecec1847e847e78d4e2c06cdc5e19d";
        private readonly string region = "westeurope";
        private readonly string voice = "Microsoft Server Speech Text to Speech Voice (en-US, JennyNeural)"; // e.g., "en-US-AriaRUS"

        public HomeController(TestServiceProxy testService)
        {
            this.testService = testService;
        }
       /* public HomeController()
        {
                
        }*/
        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> Test()
        {
        
            ViewData["Message"] = $"Hello fellow {User.FindFirst("emails").Value}!";
            var forecast = await testService.GetWeatherForecastAsync();
             return View(forecast);
             //return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult TextToSpeech()
        {
            return View();
        }
        [HttpPost]
        public IActionResult GetTextToSpeech(/*[FromBody] TongueTwister tongueTwister*/)
        {
            try
            {
                string reftext = HttpContext.Request.Form["reftext"];
                var config = SpeechConfig.FromSubscription("ebecec1847e847e78d4e2c06cdc5e19d", "westeurope");
                config.SpeechSynthesisVoiceName = "Microsoft Server Speech Text to Speech Voice (en-US, JennyNeural)";
                // Creates a speech synthesizer with a null output stream.
                // This means the audio output data will not be written to any output channel.
                // You can just get the audio from the result.
                using var audioConfig = AudioConfig.FromWavFileOutput("sound.wav");



                var synthesizer = new SpeechSynthesizer(config, audioConfig);
                var result = synthesizer.SpeakTextAsync(reftext).Result;

                if (result.Reason == ResultReason.SynthesizingAudioCompleted)
                {
                    var audioData = result.AudioData;
                    return Json(audioData);
                }

                return Json(new { success = false });
            }catch(Exception ex)
            {
                return Json(new { ex.Message });
            }
            /*  if (tongueTwister != null)
              {

              }

              return BadRequest();*/
         
        }
        [HttpPost]
        public async Task<IActionResult> GetToken()
        {
            var fetchTokenUrl = $"https://westeurope.api.cognitive.microsoft.com/sts/v1.0/issueToken";
            _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "ebecec1847e847e78d4e2c06cdc5e19d");

            var response = await _httpClient.PostAsync(fetchTokenUrl, null);
            var accessToken = await response.Content.ReadAsStringAsync();

            return new JsonResult(new { at = accessToken });
        }
        [HttpPost]
        public async Task<IActionResult> ackaud()
        {
            try
            {
                // Get the reference text from the request form
                string reftext = HttpContext.Request.Form["reftext"];

                // Get the audio data from the request files
                var audioFile = HttpContext.Request.Form.Files["audio_data"];

                if (audioFile == null)
                {
                    return BadRequest("Audio file not found in the request.");
                }

                // Convert audio data to a byte array
                byte[] audioBytes;
                using (var memoryStream = new MemoryStream())
                {
                    await audioFile.CopyToAsync(memoryStream);
                    audioBytes = memoryStream.ToArray();
                }

                // Build pronunciation assessment parameters
                string referenceText = reftext;
                string pronAssessmentParamsJson = "{\"ReferenceText\":\"" + referenceText + "\",\"GradingSystem\":\"HundredMark\",\"Dimension\":\"Comprehensive\",\"EnableMiscue\":\"True\"}";
                string pronAssessmentParamsBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(pronAssessmentParamsJson));
                string pronAssessmentParams = pronAssessmentParamsBase64;

                // Build request
                string region = "westeurope";
                string language = "en-US";
                string subscriptionKey = "ebecec1847e847e78d4e2c06cdc5e19d";
                string url = $"https://{region}.stt.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1?language={language}&usePipelineVersion=0";                             

                _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                _httpClient.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
                _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
                _httpClient.DefaultRequestHeaders.Add("Pronunciation-Assessment", pronAssessmentParams);
                _httpClient.DefaultRequestHeaders.Add("Transfer-Encoding", "chunked");
                _httpClient.DefaultRequestHeaders.Add("Expect", "100-continue");

                HttpContent httpContent = new ByteArrayContent(audioBytes);
                HttpResponseMessage response = await _httpClient.PostAsync(url, httpContent);

                // Handle response
                string responseJson = await response.Content.ReadAsStringAsync();
                return Ok(responseJson);
            }
            catch (Exception ex)
            {
                return new JsonResult(ex);
            }
        }
        [HttpPost]
        public async Task<IActionResult> getttsforword()
        {
            try
            {
                string word = HttpContext.Request.Form["word"];

                // Creates an instance of a speech config with the specified subscription key and service region.
                var speechConfig = SpeechConfig.FromSubscription(subscriptionKey, region);
                speechConfig.SpeechSynthesisVoiceName = voice;

                // Creates a speech synthesizer with a null output stream.
                // This means the audio output data will not be written to any output channel.
                // You can just get the audio from the result.
                using var audioConfig = AudioConfig.FromWavFileOutput("sound.wav");

                using var synthesizer = new SpeechSynthesizer(speechConfig, audioConfig);
                var result = await synthesizer.SpeakSsmlAsync($"<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'><voice name='{voice}'>{word}</voice></speak>");

                // Check result
                if (result.Reason == ResultReason.SynthesizingAudioCompleted)
                {
                    var audioBytes = await System.IO.File.ReadAllBytesAsync("sound.wav");

                    // Delete the temporary WAV file
                    System.IO.File.Delete("sound.wav");

                    var response = File(audioBytes, "audio/wav", "sound.wav");
                    return response;
                }
                else if (result.Reason == ResultReason.Canceled)
                {
                    var cancellationDetails = SpeechSynthesisCancellationDetails.FromResult(result);
                    Console.WriteLine($"Speech synthesis canceled: {cancellationDetails.Reason}");
                    if (cancellationDetails.Reason == CancellationReason.Error)
                    {
                        Console.WriteLine($"Error details: {cancellationDetails.ErrorDetails}");
                    }
                }

                return BadRequest("Failed to generate TTS audio for the word.");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
        }

        [HttpPost]
        [Route("enroll-verify-speaker")]
        public async Task<IActionResult> VerificationEnroll()
        {
            var config = SpeechConfig.FromSubscription("df7b1048a28c4355889acf6278469d5f", "eastus");
            var profileMapping = new Dictionary<string, string>();

            using (var client = new VoiceProfileClient(config))
            using (var profile = await client.CreateProfileAsync(VoiceProfileType.TextDependentVerification, "en-us"))
            {
                var phraseResult = await client.GetActivationPhrasesAsync(VoiceProfileType.TextDependentVerification, "en-us");
                using var audioInput = AudioConfig.FromDefaultMicrophoneInput();
                Console.WriteLine($"Enrolling profile id {profile.Id}.");
                // give the profile a human-readable display name
                profileMapping.Add(profile.Id, "Yasser");

                VoiceProfileEnrollmentResult result = null;
                while (result is null || result.RemainingEnrollmentsCount > 0)
                {
                    Console.WriteLine($"Speak the passphrase, \"${phraseResult.Phrases[0]}\"");
                    result = await client.EnrollProfileAsync(profile, audioInput);
                    Console.WriteLine($"Remaining enrollments needed: {result.RemainingEnrollmentsCount}");
                    Console.WriteLine("");
                    return Json(new { success = false, phraseCounter = result.RemainingEnrollmentsCount, phrase = phraseResult.Phrases[0] });

                }

                if (result.Reason == ResultReason.EnrolledVoiceProfile)
                {
                    var speakerRecognizer = new SpeakerRecognizer(config, AudioConfig.FromDefaultMicrophoneInput());
                    var model = SpeakerVerificationModel.FromProfile(profile);

                    Console.WriteLine("Speak the passphrase to verify: \"My voice is my passport, please verify me.\"");
                    var res = await speakerRecognizer.RecognizeOnceAsync(model);
                    Console.WriteLine($"Verified voice profile for speaker {profileMapping[res.ProfileId]}, score is {res.Score}");
                    return Ok(new { success = true, score = res.Score });

                }
                else if (result.Reason == ResultReason.Canceled)
                {
                    var cancellation = VoiceProfileEnrollmentCancellationDetails.FromResult(result);
                    Console.WriteLine($"CANCELED {profile.Id}: ErrorCode={cancellation.ErrorCode} ErrorDetails={cancellation.ErrorDetails}");
                    return Json(new { success = false });

                }
            }
            return Ok(new { success = true });

        }



    }
}
