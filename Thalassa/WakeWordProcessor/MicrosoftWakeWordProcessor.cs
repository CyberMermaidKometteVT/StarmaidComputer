using System.Speech.Recognition;

using Microsoft.Extensions.Logging;

using StarmaidIntegrationComputer.Common.Settings;
using StarmaidIntegrationComputer.Thalassa.Settings;
using StarmaidIntegrationComputer.Thalassa.VoiceToText;

namespace StarmaidIntegrationComputer.Thalassa.WakeWordProcessor
{
    public class MicrosoftWakeWordProcessor : WakeWordProcessorBase
    {
        SpeechRecognitionEngine recognitionEngine = new SpeechRecognitionEngine();
        private readonly ILogger<WakeWordProcessorBase> logger;
        private readonly StreamerProfileSettings streamerProfileSettings;
        private readonly ThalassaSettings thalassaSettings;

        public MicrosoftWakeWordProcessor(ILogger<WakeWordProcessorBase> logger, StreamerProfileSettings streamerProfileSettings, ThalassaSettings thalassaSettings) : base(logger, streamerProfileSettings)
        {
            this.logger = logger;
            this.streamerProfileSettings = streamerProfileSettings;
            this.thalassaSettings = thalassaSettings;

            //Find sound-alike words at https://www.rhymezone.com/r/rhyme.cgi?org1=syl&org2=l&typeofrhyme=sim&Word=Thalassa
            #region All the words the wake word interpreter should know
            List<string> commonEnglishWords = new List<string> { "be", "and", "of", "a", "in", "to", "have", "too", "it", "I", "that", "for", "you", "he", "with", "on", "do", "say", "this", "they", "at", "but", "we", "his", "from", "that", "not", "can’t", "won’t", "by", "she", "or", "as", "what", "go", "their", "can", "who", "get", "if", "would", "her", "all", "my", "make", "about", "know", "will", "as", "up", "one", "time", "there", "year", "so", "think", "when", "which", "them", "some", "me", "people", "take", "out", "into", "just", "see", "him", "your", "come", "could", "now", "than", "like", "other", "how", "then", "its", "our", "two", "more", "these", "want", "way", "look", "first", "also", "new", "because", "day", "more", "use", "no", "man", "find", "here", "thing", "give", "many", "well", "only", "those", "tell", "one", "very", "her", "even", "back", "any", "good", "woman", "through", "us", "life", "child", "there", "work", "down", "may", "after", "should", "call", "world", "over", "school", "still", "try", "in", "as", "last", "ask", "need", "too", "feel", "three", "when", "state", "never", "become", "between", "high", "really", "something", "most", "another", "much", "family", "own", "out", "leave", "put", "old", "while", "mean", "on", "keep", "student", "why", "let", "great", "same", "big", "group", "begin", "seem", "country", "help", "talk", "where", "turn", "problem", "every", "start", "hand", "might", "American", "show", "part", "about", "against", "place", "over", "such", "again", "few", "case", "most", "week", "company", "where", "system", "each", "right", "program", "hear", "so", "question", "during", "work", "play", "government", "run", "small", "number", "off", "always", "move", "like", "night", "live", "Mr.", "point", "believe", "hold", "today", "bring", "happen", "next", "without", "before", "large", "all", "million", "must", "home", "under", "water", "room", "write", "mother", "area", "national", "money", "story", "young", "fact", "month", "different", "lot", "right", "study", "book", "eye", "job", "word", "though", "business", "issue", "side", "kind", "four", "head", "far", "black", "long", "both", "little", "house", "yes", "after", "since", "long", "provide", "service", "around", "friend", "important", "father", "sit", "away", "until", "power", "hour", "game", "often", "yet", "line", "political", "end", "among", "ever", "stand", "bad", "lose", "however", "member", "pay", "law", "meet", "car", "city", "almost", "include", "continue", "set", "later", "community", "much", "name", "five", "once", "white", "least", "president", "learn", "real", "change", "team", "minute", "best", "several", "idea", "kid", "body", "information", "nothing", "ago", "right", "lead", "social", "understand", "whether", "back", "watch", "together", "follow", "around", "parent", "only", "stop", "face", "anything", "create", "public", "already", "speak", "others", "read", "level", "allow", "add", "office", "spend", "door", "health", "person", "art", "sure", "such", "war", "history", "party", "within", "grow", "result", "open", "change", "morning", "walk", "reason", "low", "win", "research", "girl", "guy", "early", "food", "before", "moment", "himself", "air", "teacher", "force", "offer", "enough", "both", "education", "across", "although", "remember", "foot", "second", "boy", "maybe", "toward", "able", "age", "off", "policy", "everything", "love", "process", "music", "including", "consider", "appear", "actually", "buy", "probably", "human", "wait", "serve", "market", "die", "send", "expect", "home", "sense", "build", "stay", "fall", "oh", "nation", "plan", "cut", "college", "interest", "death", "course", "someone", "experience", "behind", "reach", "local", "kill", "six", "remain", "effect", "use", "yeah", "suggest", "class", "control", "raise", "care", "perhaps", "little", "late", "hard", "field", "else", "pass", "former", "sell", "major", "sometimes", "require", "along", "development", "themselves", "report", "role", "better", "economic", "effort", "up", "decide", "rate", "strong", "possible", "heart", "drug", "show", "leader", "light", "voice", "wife", "whole", "police", "mind", "finally", "pull", "return", "free", "military", "price", "report", "less", "according", "decision", "explain", "son", "hope", "even", "develop", "view", "relationship", "carry", "town", "road", "drive", "arm", "TRUE", "federal", "break", "better", "difference", "thank", "receive", "value", "international", "building", "action", "full", "model", "join", "season", "society", "because", "tax", "director", "early", "position", "player", "agree", "especially", "record", "pick", "wear", "paper", "special", "space", "ground", "form", "support", "event", "official", "whose", "matter", "everyone", "center", "couple", "site", "end", "project", "hit", "base", "activity", "star", "table", "need", "court", "produce", "eat", "American", "teach", "oil", "half", "situation", "easy", "cost", "industry", "figure", "face", "street", "image", "itself", "phone", "either", "data", "cover", "quite", "picture", "clear", "practice", "piece", "land", "recent", "describe", "product", "doctor", "wall", "patient", "worker", "news", "test", "movie", "certain", "north", "love", "personal", "open", "support", "simply", "third", "technology", "catch", "step", "baby", "computer", "type", "attention", "draw", "film", "Republican", "tree", "source", "red", "nearly", "organization", "choose", "cause", "hair", "look", "point  “What is the point of all this?", "century", "evidence", "window", "difficult  “Sometimes, life can be difficult.”", "soon", "culture", "billion", "chance", "brother", "energy", "period", "course", "summer", "less", "realize", "hundred", "available", "plant", "likely", "opportunity", "term", "short", "letter", "condition", "choice", "place", "single", "rule", "daughter", "administration", "south", "husband", "Congress", "floor", "campaign", "material", "population", "well", "call", "economy", "medical -“She needs medical assistance.”", "hospital", "church", "close -“Please close the door.”", /*"thousand",*/ "risk", "current", "fire", "future -“The future is full of hope.”", "wrong", "involve", "defense", "anyone", "increase", "security", "bank", "myself", "certainly", "west", "sport", "board", "seek", "per", "subject", "officer", "private", "rest", "behavior", "deal", "performance", "fight", "throw", "top", "quickly", "past", "goal", "second", "bed", "order", "author", "fill", "represent", "focus", "foreign", "drop", "plan", "blood", "upon", "agency", "push", "nature", "color", "no", "recently", "store", "reduce", "sound", "note", "fine", "before", "near", "movement", "page", "enter", "share", "than", "common", "poor", "other", "natural", "race", "concern", "series", "significant", "similar", "hot", "language", "each", "usually", "response", "dead", "rise", "animal", "factor", "decade", "article", "shoot", "east", "save", "seven", "artist", "away", "scene", "stock", "career", "despite", "central", "eight", "thus", "treatment", "beyond", "happy", "exactly", "protect", "approach", "lie", "size", "dog", "fund", "serious", "occur", "media", "ready", "sign", "thought", "list", "individual", "simple", "quality", "pressure", "accept", "answer", "hard", "resource", "identify", "left", "meeting", "determine", "prepare", "disease", "whatever", "success", "argue", "cup", "particularly", "amount", "ability", "staff", "recognize", "indicate", "character", "growth", "loss", "degree", "wonder", "attack", "herself", "region", "television", "box", "TV", "training", "pretty", "trade", "deal", "election", "everybody", "physical", "lay", "general", "feeling", "standard", "bill", "message", "fail", "outside", "arrive", "analysis", "benefit", "name", "sex", "forward", "lawyer", "present", "section", "environmental", "glass", "answer", "skill", "sister", "PM", "professor", "operation", "financial", "crime", "stage", "ok", "compare", "authority", "miss", "design", "sort", "one", "act", "ten", "knowledge", "gun", "station", "blue", "state", "strategy", "little", "clearly", "discuss", "indeed", "force", "truth", "song", "example", "democratic", "check", "environment", "leg", "dark", "public", "various", "rather", "laugh", "guess", "executive", "set", "study", "prove", "hang", "entire", "rock", "design", "enough", "forget", "since", "claim", "note", "remove", "manager", "help", "close", "sound", "enjoy", "network", "legal", "religious", "cold", "form", "final", "main", "science", "green", "memory", "card", "above", "seat", "cell", "establish", "nice", "trial", "expert", "that", "spring", "firm", "Democrat", "radio", "visit", "management", "care", "avoid", "imagine", "tonight", "huge", "ball", "no", "close", "finish", "yourself", "talk", "theory", "impact", "respond", "statement", "maintain", "charge", "popular", "traditional", "onto", "reveal", "direction", "weapon", "employee", "cultural", "contain", "peace", "head", "control", "base", "pain", "apply", "play", "measure", "wide", "shake", "fly", "interview", "manage", "chair", "fish", "particular", "camera", "structure", "politics", "perform", "bit", "weight", "suddenly", "discover", "candidate", "top", "production", "treat", "trip", "evening", "affect", "inside", "conference", "unit", "best", "style", "adult", "worry", "range", "mention", "rather", "far", "deep", "front", "edge", "individual", "specific", "writer", "trouble", "necessary", "throughout", "challenge", "fear", "shoulder", "institution", "middle", "sea", "dream", "bar", "beautiful", "property", "instead", "improve", "stuff", "claim", };

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

        public override void Dispose()
        {
            recognitionEngine.Dispose();
        }

        public override void StartListening()
        {
            recognitionEngine.RecognizeAsync(RecognizeMode.Multiple);
            IsListening = true;
        }

        public override void StopListening()
        {
            recognitionEngine.RecognizeAsyncStop();

            recognitionEngine.RecognizeAsyncStop();
            logger.LogInformation($"Sleeping, not listening for the wake word...");
        }
        private void Recognizer_SpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
        {
            //#error Figure out how to get this into the Porcupine proccessor also - if that's possible. (It's built for the Windows one.)
            string textToDisplay = $"({e.Result.Confidence}): {e.Result.Text}";
            logger.LogInformation($"{streamerProfileSettings.AiName} Speech Recognized: {textToDisplay}");

            DisplayIfAble(textToDisplay);

            if (e.Result.Confidence > thalassaSettings.WakeWordConfidenceThreshold && streamerProfileSettings.WakeWords.Contains(e.Result.Text))
            {
                logger.LogInformation($"Wake word identified");
                OnWakeWordHeard();
                return;
            }

            if (e.Result.Confidence > thalassaSettings.CancelListeningConfidenceThreshold && streamerProfileSettings.CancelListeningPhrases.Contains(e.Result.Text))
            {
                logger.LogInformation($"Cancel listening phrase identified! Stopping listening!");
                OnCancelListeningHeard();
                return;
            }

            if (e.Result.Confidence > thalassaSettings.AbortCommandConfidenceThreshold && streamerProfileSettings.AbortCommandPhrases.Contains(e.Result.Text))
            {
                logger.LogInformation($"Abort command phrase identified!");
                OnAbortCommandHeard();
                return;
            }
        }

        private void RecognitionEngine_SpeechRecognitionRejected(object? sender, SpeechRecognitionRejectedEventArgs e)
        {
            string textToDisplay = $"({e.Result.Confidence}): {e.Result.Text}";
            logger.LogInformation($"{streamerProfileSettings.AiName} REJECTED Speech: {textToDisplay}");

            DisplayIfAble(textToDisplay);
        }
    }
}
