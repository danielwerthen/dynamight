using Microsoft.Kinect;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;
using System.Speech.Synthesis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dynamight.Processing.Audio
{
    public class VoiceCommander
    {
        SpeechSynthesizer synth;
        SpeechRecognitionEngine engine;
        public VoiceCommander(KinectSensor sensor)
        {
            synth = new SpeechSynthesizer();
            engine = CreateRecognizer(sensor);
            engine.BabbleTimeout = new TimeSpan(0, 1, 0);
        }

        public void Prompt(string prompt)
        {
            synth.Speak(new Prompt(prompt));
        }

        public void LoadChoices(params VoiceCommand[] commands)
        {
            var choices = new Choices();
            foreach (var command in commands)
            {
                if (command.Semantics == null || command.Semantics.Length == 0)
                    choices.Add(new SemanticResultValue(command.Word, command.Word));
                else
                {
                    foreach (var sem in command.Semantics)
                        choices.Add(new SemanticResultValue(sem, command.Word));
                }
            }
            var gb = new GrammarBuilder { Culture = engine.RecognizerInfo.Culture };
            gb.Append(choices);
            engine.LoadGrammar(new Grammar(gb));
        }

        public string Recognize(string prompt = "GO", string onNull = "Speak up, please!")
        {
            if (prompt != null)
                Prompt(prompt);
            var result = engine.Recognize();
            if (result == null)
            {
                if (onNull != null)
                    return Recognize(prompt, onNull);
                return null;
            }
            return result.Text;
        }

        private static SpeechRecognitionEngine CreateRecognizer(KinectSensor sensor)
        {
            RecognizerInfo rec = SpeechRecognitionEngine.InstalledRecognizers()
                .Where(ri => ri.AdditionalInfo.ContainsKey("Kinect") && ri.AdditionalInfo["Kinect"] == "True")
                .FirstOrDefault();
            if (rec == null)
                return null;
            var engine = new SpeechRecognitionEngine(rec.Id);
            if (!sensor.IsRunning)
                sensor.Start();
            sensor.AudioSource.EchoCancellationMode = EchoCancellationMode.None;
            sensor.AudioSource.AutomaticGainControlEnabled = false;
            engine.SetInputToAudioStream(sensor.AudioSource.Start(), new Microsoft.Speech.AudioFormat.SpeechAudioFormatInfo(EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
            return engine;
        }
    }

    public class VoiceCommand
    {
        public string Word;
        public string[] Semantics = new string[0];
        public static implicit operator VoiceCommand(string word)
        {
            return new VoiceCommand { Word = word };
        }
    }
}
