﻿using System.Speech.Recognition;

using Microsoft.Extensions.Logging;

using StarmaidIntegrationComputer.Common.DataStructures.StarmaidState;
using StarmaidIntegrationComputer.Common.Settings;
using StarmaidIntegrationComputer.Common.TasksAndExecution;
using StarmaidIntegrationComputer.Thalassa.Settings;
using StarmaidIntegrationComputer.Thalassa.SpeechSynthesis;
using StarmaidIntegrationComputer.Thalassa.VoiceToText;

namespace StarmaidIntegrationComputer.Thalassa
{
    public class ThalassaCore : IDisposable
    {
        private readonly VoiceToTextManager voiceToTextManager;
        private readonly ThalassaSettings settings;
        private readonly StarmaidStateBag stateBag;
        private readonly StreamerProfileSettings streamerProfileSettings;
        private readonly SpeechComputer speechComputer;
        SpeechRecognitionEngine recognitionEngine = new SpeechRecognitionEngine();

        private bool isListening = false;

        public bool IsListening
        {
            get { return isListening; }
            set
            {
                isListening = value;
                IsListeningChangedHandlers.Invoke();
            }
        }

        public Action<string>? DisplayInput { get; set; }
        public Action<string>? SpeechInterpreted { get; set; }
        public Action? AbortCommandIssued { get; set; }

        private ILogger<ThalassaCore>? Logger { get; set; }

        private ILoggerFactory? loggerFactory;
        public ILoggerFactory? LoggerFactory
        {
            get { return loggerFactory; }
            set
            {
                loggerFactory = value;
                Logger = loggerFactory?.CreateLogger<ThalassaCore>();
            }
        }

        public List<Action> StartingListeningHandlers { get; } = new List<Action>();
        public List<Action> StoppingListeningHandlers { get; } = new List<Action>();
        public List<Action> IsListeningChangedHandlers { get; } = new List<Action>();

        public ThalassaCore(VoiceToTextManager voiceToTextManager, ThalassaSettings settings, StarmaidStateBag stateBag, StreamerProfileSettings streamerProfileSettings, SpeechComputer speechComputer)
        {
            this.voiceToTextManager = voiceToTextManager;
            this.settings = settings;
            this.stateBag = stateBag;
            this.streamerProfileSettings = streamerProfileSettings;
            this.speechComputer = speechComputer;

            //Find sound-alike words at https://www.rhymezone.com/r/rhyme.cgi?org1=syl&org2=l&typeofrhyme=sim&Word=Thalassa
            #region All the words the wake word interpreter should know
            List<string> commonEnglishWords = new List<string> { "be", "and", "of", "a", "in", "to", "have", "too", "it", "I", "that", "for", "you", "he", "with", "on", "do", "say", "this", "they", "at", "but", "we", "his", "from", "that", "not", "can’t", "won’t", "by", "she", "or", "as", "what", "go", "their", "can", "who", "get", "if", "would", "her", "all", "my", "make", "about", "know", "will", "as", "up", "one", "time", "there", "year", "so", "think", "when", "which", "them", "some", "me", "people", "take", "out", "into", "just", "see", "him", "your", "come", "could", "now", "than", "like", "other", "how", "then", "its", "our", "two", "more", "these", "want", "way", "look", "first", "also", "new", "because", "day", "more", "use", "no", "man", "find", "here", "thing", "give", "many", "well", "only", "those", "tell", "one", "very", "her", "even", "back", "any", "good", "woman", "through", "us", "life", "child", "there", "work", "down", "may", "after", "should", "call", "world", "over", "school", "still", "try", "in", "as", "last", "ask", "need", "too", "feel", "three", "when", "state", "never", "become", "between", "high", "really", "something", "most", "another", "much", "family", "own", "out", "leave", "put", "old", "while", "mean", "on", "keep", "student", "why", "let", "great", "same", "big", "group", "begin", "seem", "country", "help", "talk", "where", "turn", "problem", "every", "start", "hand", "might", "American", "show", "part", "about", "against", "place", "over", "such", "again", "few", "case", "most", "week", "company", "where", "system", "each", "right", "program", "hear", "so", "question", "during", "work", "play", "government", "run", "small", "number", "off", "always", "move", "like", "night", "live", "Mr.", "point", "believe", "hold", "today", "bring", "happen", "next", "without", "before", "large", "all", "million", "must", "home", "under", "water", "room", "write", "mother", "area", "national", "money", "story", "young", "fact", "month", "different", "lot", "right", "study", "book", "eye", "job", "word", "though", "business", "issue", "side", "kind", "four", "head", "far", "black", "long", "both", "little", "house", "yes", "after", "since", "long", "provide", "service", "around", "friend", "important", "father", "sit", "away", "until", "power", "hour", "game", "often", "yet", "line", "political", "end", "among", "ever", "stand", "bad", "lose", "however", "member", "pay", "law", "meet", "car", "city", "almost", "include", "continue", "set", "later", "community", "much", "name", "five", "once", "white", "least", "president", "learn", "real", "change", "team", "minute", "best", "several", "idea", "kid", "body", "information", "nothing", "ago", "right", "lead", "social", "understand", "whether", "back", "watch", "together", "follow", "around", "parent", "only", "stop", "face", "anything", "create", "public", "already", "speak", "others", "read", "level", "allow", "add", "office", "spend", "door", "health", "person", "art", "sure", "such", "war", "history", "party", "within", "grow", "result", "open", "change", "morning", "walk", "reason", "low", "win", "research", "girl", "guy", "early", "food", "before", "moment", "himself", "air", "teacher", "force", "offer", "enough", "both", "education", "across", "although", "remember", "foot", "second", "boy", "maybe", "toward", "able", "age", "off", "policy", "everything", "love", "process", "music", "including", "consider", "appear", "actually", "buy", "probably", "human", "wait", "serve", "market", "die", "send", "expect", "home", "sense", "build", "stay", "fall", "oh", "nation", "plan", "cut", "college", "interest", "death", "course", "someone", "experience", "behind", "reach", "local", "kill", "six", "remain", "effect", "use", "yeah", "suggest", "class", "control", "raise", "care", "perhaps", "little", "late", "hard", "field", "else", "pass", "former", "sell", "major", "sometimes", "require", "along", "development", "themselves", "report", "role", "better", "economic", "effort", "up", "decide", "rate", "strong", "possible", "heart", "drug", "show", "leader", "light", "voice", "wife", "whole", "police", "mind", "finally", "pull", "return", "free", "military", "price", "report", "less", "according", "decision", "explain", "son", "hope", "even", "develop", "view", "relationship", "carry", "town", "road", "drive", "arm", "TRUE", "federal", "break", "better", "difference", "thank", "receive", "value", "international", "building", "action", "full", "model", "join", "season", "society", "because", "tax", "director", "early", "position", "player", "agree", "especially", "record", "pick", "wear", "paper", "special", "space", "ground", "form", "support", "event", "official", "whose", "matter", "everyone", "center", "couple", "site", "end", "project", "hit", "base", "activity", "star", "table", "need", "court", "produce", "eat", "American", "teach", "oil", "half", "situation", "easy", "cost", "industry", "figure", "face", "street", "image", "itself", "phone", "either", "data", "cover", "quite", "picture", "clear", "practice", "piece", "land", "recent", "describe", "product", "doctor", "wall", "patient", "worker", "news", "test", "movie", "certain", "north", "love", "personal", "open", "support", "simply", "third", "technology", "catch", "step", "baby", "computer", "type", "attention", "draw", "film", "Republican", "tree", "source", "red", "nearly", "organization", "choose", "cause", "hair", "look", "point  “What is the point of all this?", "century", "evidence", "window", "difficult  “Sometimes, life can be difficult.”", "listen", "soon", "culture", "billion", "chance", "brother", "energy", "period", "course", "summer", "less", "realize", "hundred", "available", "plant", "likely", "opportunity", "term", "short", "letter", "condition", "choice", "place", "single", "rule", "daughter", "administration", "south", "husband", "Congress", "floor", "campaign", "material", "population", "well", "call", "economy", "medical -“She needs medical assistance.”", "hospital", "church", "close -“Please close the door.”", "thousand", "risk", "current", "fire", "future -“The future is full of hope.”", "wrong", "involve", "defense", "anyone", "increase", "security", "bank", "myself", "certainly", "west", "sport", "board", "seek", "per", "subject", "officer", "private", "rest", "behavior", "deal", "performance", "fight", "throw", "top", "quickly", "past", "goal", "second", "bed", "order", "author", "fill", "represent", "focus", "foreign", "drop", "plan", "blood", "upon", "agency", "push", "nature", "color", "no", "recently", "store", "reduce", "sound", "note", "fine", "before", "near", "movement", "page", "enter", "share", "than", "common", "poor", "other", "natural", "race", "concern", "series", "significant", "similar", "hot", "language", "each", "usually", "response", "dead", "rise", "animal", "factor", "decade", "article", "shoot", "east", "save", "seven", "artist", "away", "scene", "stock", "career", "despite", "central", "eight", "thus", "treatment", "beyond", "happy", "exactly", "protect", "approach", "lie", "size", "dog", "fund", "serious", "occur", "media", "ready", "sign", "thought", "list", "individual", "simple", "quality", "pressure", "accept", "answer", "hard", "resource", "identify", "left", "meeting", "determine", "prepare", "disease", "whatever", "success", "argue", "cup", "particularly", "amount", "ability", "staff", "recognize", "indicate", "character", "growth", "loss", "degree", "wonder", "attack", "herself", "region", "television", "box", "TV", "training", "pretty", "trade", "deal", "election", "everybody", "physical", "lay", "general", "feeling", "standard", "bill", "message", "fail", "outside", "arrive", "analysis", "benefit", "name", "sex", "forward", "lawyer", "present", "section", "environmental", "glass", "answer", "skill", "sister", "PM", "professor", "operation", "financial", "crime", "stage", "ok", "compare", "authority", "miss", "design", "sort", "one", "act", "ten", "knowledge", "gun", "station", "blue", "state", "strategy", "little", "clearly", "discuss", "indeed", "force", "truth", "song", "example", "democratic", "check", "environment", "leg", "dark", "public", "various", "rather", "laugh", "guess", "executive", "set", "study", "prove", "hang", "entire", "rock", "design", "enough", "forget", "since", "claim", "note", "remove", "manager", "help", "close", "sound", "enjoy", "network", "legal", "religious", "cold", "form", "final", "main", "science", "green", "memory", "card", "above", "seat", "cell", "establish", "nice", "trial", "expert", "that", "spring", "firm", "Democrat", "radio", "visit", "management", "care", "avoid", "imagine", "tonight", "huge", "ball", "no", "close", "finish", "yourself", "talk", "theory", "impact", "respond", "statement", "maintain", "charge", "popular", "traditional", "onto", "reveal", "direction", "weapon", "employee", "cultural", "contain", "peace", "head", "control", "base", "pain", "apply", "play", "measure", "wide", "shake", "fly", "interview", "manage", "chair", "fish", "particular", "camera", "structure", "politics", "perform", "bit", "weight", "suddenly", "discover", "candidate", "top", "production", "treat", "trip", "evening", "affect", "inside", "conference", "unit", "best", "style", "adult", "worry", "range", "mention", "rather", "far", "deep", "front", "edge", "individual", "specific", "writer", "trouble", "necessary", "throughout", "challenge", "fear", "shoulder", "institution", "middle", "sea", "dream", "bar", "beautiful", "property", "instead", "improve", "stuff", "claim", };

            IEnumerable<string> wakeWordSystemVocabulary = new List<string>(commonEnglishWords)
                .Union(streamerProfileSettings.WakeWordSoundalikes)
                .Union(streamerProfileSettings.WakeWords)
                .Union(streamerProfileSettings.CancelListeningPhrases)
                .Union(streamerProfileSettings.AbortCommandPhrases)
                ;

            #endregion All the words the wake word interpreter should know
            Choices choices = new Choices(wakeWordSystemVocabulary.ToArray());
            var builder = choices.ToGrammarBuilder();

            recognitionEngine.LoadGrammar(new Grammar(builder));
            recognitionEngine.SpeechRecognized += Recognizer_SpeechRecognized;
            recognitionEngine.SpeechRecognitionRejected += RecognitionEngine_SpeechRecognitionRejected;
            recognitionEngine.SetInputToDefaultAudioDevice();
            recognitionEngine.EndSilenceTimeout = TimeSpan.FromMilliseconds(50);
        }

        public void Dispose()
        {
            recognitionEngine?.Dispose();
            recognitionEngine = null;
        }

        public void StartListening()
        {
            if (recognitionEngine == null)
            {
                throw new InvalidOperationException($"Error - trying to use a disposed {GetType().Name}!  Get a new one!");
            }

            if (IsListening)
            {
                return;
            }

            recognitionEngine.RecognizeAsync(RecognizeMode.Multiple);
            IsListening = true;

            Logger.LogInformation($"{streamerProfileSettings.AiName} is listening...");
        }

        public void StopListening()
        {
            if (recognitionEngine == null)
            {
                return;
            }

            if (!IsListening)
            {
                return;
            }

            recognitionEngine.RecognizeAsyncStop();
            IsListening = false;

            Logger.LogInformation($"{streamerProfileSettings.AiName} is sleeping...");
        }

        public void AbortCurrentListening()
        {
            voiceToTextManager.AbortCurrentListening();
        }

        public void ConcludeCurrentListening()
        {
            voiceToTextManager.ConcludeCurrentListening();
        }

        public void CancelSpeech()
        {
            speechComputer.CancelSpeech();
        }


        private void Recognizer_SpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
        {
            string textToDisplay = $"({e.Result.Confidence}): {e.Result.Text}";
            Logger.LogInformation($"{streamerProfileSettings.AiName} Speech Recognized: {textToDisplay}");

            DisplayIfAble(textToDisplay);

            if (e.Result.Confidence > settings.WakeWordConfidenceThreshold && streamerProfileSettings.WakeWords.Contains(e.Result.Text))
            {
                Logger.LogInformation($"Wake word identified!  Starting to listen to what comes next!");

                string raidersAsString = string.Join(", ", stateBag.Raiders.Select(raider => raider.RaiderName));
                string chattersAsString = string.Join(", ", stateBag.Chatters.Select(chatter => chatter.ChatterName));
                string viewersAsString = string.Join(", ", stateBag.Viewers);
                string context = $"This transcript is interpreting the spoken communication between {streamerProfileSettings.StreamerName}, {streamerProfileSettings.StreamerDescription}. They will be talking to {streamerProfileSettings.AiName}, their {streamerProfileSettings.AiDescription}. {streamerProfileSettings.StreamerName} is {streamerProfileSettings.StreamerMetaDescription}. It is important that the transcription correctly identify usernames, which might have weird capitalization, numbers, and special characters.  Those special characters might sound like the word describing those character like \"ampersand\" for &, they might sound like other letters they're meant to represent like \"L\" for \"1\", or might be entirely silent, like an \"_\"!  Also, consider raiders before chatters, and chatters before viewers.\r\nRecent raiders: {raidersAsString}\r\nRecent chatters: {chattersAsString}\r\nCurrent viewers: {viewersAsString}\r\n\r\nHere are some examples, which will be in the format: \"PROMPT: (prompt) | INPUT: (speech that's been interpreted to text) | OUTPUT: (the expected response)\r\nPROMPT: Current viewers: CyberMermaidKomette_VT | INPUT: Cyber Mermaid Komette VT | OUTPUT: CyberMermaidKomette_VT\r\nPROMPT: Recent chatters: damien_verde_ch | INPUT: Damien Verde C H | damien_verde_ch";


                Logger.LogInformation($"Passing in to our voice to text manager the following context: \r\n{context}");

                var result = voiceToTextManager.StartListeningAndInterpret(context).ContinueWith(ReactToSpeech);
                StartingListeningHandlers.Invoke();
            }

            if (e.Result.Confidence > settings.CancelListeningConfidenceThreshold && streamerProfileSettings.CancelListeningPhrases.Contains(e.Result.Text))
            {
                Logger.LogInformation($"Cancel listening phrase identified! Stopping listening!");

                AbortCurrentListening();
            }

            if (e.Result.Confidence > settings.AbortCommandConfidenceThreshold && streamerProfileSettings.AbortCommandPhrases.Contains(e.Result.Text))
            {

                if (AbortCommandIssued != null)
                {
                    Logger.LogInformation($"Abort command phrase identified! Aborting command!");
                    AbortCommandIssued();
                }
                else
                {
                    Logger.LogInformation($"Abort command phrase identified, but its behavior has not been wired up!");
                }
            }
        }

        private void ReactToSpeech(Task<string> completeInterpretationTask)
        {
            StoppingListeningHandlers.Invoke();

            //Bad case! :(
            if (completeInterpretationTask.Exception != null)
            {
                string errorMessage = $"Error interpreting speech! Error message: {completeInterpretationTask.Exception.Message}";
                Logger.LogError(completeInterpretationTask.Exception, errorMessage);
                DisplayIfAble(errorMessage);
                return;
            }

            //Bypassing case: Abort issued
            if (completeInterpretationTask.Status == TaskStatus.Canceled)
            {
                string abortMessage = $"ABORT ISSUED, NOT INTERPRETING SPEECH";
                Logger.LogInformation(abortMessage);
                DisplayIfAble(abortMessage);
                return;
            }

            Logger.LogInformation($"Speech after the wake word: {completeInterpretationTask.Result}");

            //Bypassing case: Already listening, not requeuing!  ^.^,
            if (completeInterpretationTask.Result == VoiceToTextManager.ALREADY_LISTENING_RESULT)
            {
                Logger.LogInformation($"Skipping interpreting the speech, as we were already interpreting it in a different task!`");
                return;
            }

            //Good case! :D
            DisplayIfAble($"OpenAI heard: {completeInterpretationTask.Result}");
            SpeechInterpreted(completeInterpretationTask.Result);
        }

        private void RecognitionEngine_SpeechRecognitionRejected(object? sender, SpeechRecognitionRejectedEventArgs e)
        {

            string textToDisplay = $"({e.Result.Confidence}): {e.Result.Text}";
            Logger.LogInformation($"{streamerProfileSettings.AiName} REJECTED Speech: {textToDisplay}");

            DisplayIfAble(textToDisplay);
        }

        private void DisplayIfAble(string textToDisplay)
        {
            if (DisplayInput != null)
            {
                DisplayInput(textToDisplay);
                //Consider throwing an error if DisplayInput is unset
            }
        }

    }
}