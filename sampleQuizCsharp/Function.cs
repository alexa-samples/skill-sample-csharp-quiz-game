using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using AlexaAPI.Request;
using AlexaAPI.Response;
using AlexaAPI;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializerAttribute(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace sampleQuizCsharp
{
    public class Function
    {
        private enum AppState
        {
            Start,
            Quiz
        }

        static int MAX_QUESTION = 10;
        AppState appstate = AppState.Start;
        const string STATE = "state";
        const string RESPONSE = "response";
        const string QUIZITEM = "quizitem";
        const string QUIZPROPERTY = "quizproperty";
        const string QUIZSCORE = "quizscore";
        const string COUNTER = "counter";

        //This is the welcome message for when a user starts the skill without a specific intent.
        const string WELCOME_MESSAGE = "Welcome to the United States Quiz Game!  You can ask me about any of the fifty states and their capitals, or you can ask me to start a quiz.  What would you like to do?";
        
        //This is the message a user will hear when they try to cancel or stop the skill, or when they finish a quiz.
        const string EXIT_SKILL_MESSAGE = "Thank you for playing the United States Quiz Game!  Let's play again soon!";

        //This is the message a user will hear after they ask (and hear) about a specific data element.
        const string REPROMPT_SPEECH = "Which other state or capital would you like to know about?";

        //This is the message a user will hear when they ask Alexa for help in your skill.
        const string HELP_MESSAGE = "I know lots of things about the United States.  You can ask me about a state or a capital, and I'll tell you what I know.  You can also test your knowledge by asking me to start a quiz.  What would you like to do?";

        //speech formatters
        static string space = " ";
        static string sayas_interject = "<say-as interpret-as='interjection'>";
        static string sayas_spellout = "<say-as interpret-as='spell-out'>";
        static string sayas = "</say-as>";
        static string breakstrong = "<break strength='strong'/>";

        //If you don't want to use cards in your skill, set the USE_CARDS_FLAG to false. 
        //If you set it to true, you will need an image for each item in your data.
        private bool USE_CARDS_FLAG = true;
        private SkillResponse response = null;
        private static List<Item> theItems = null;
        private int counter = 0;
        private int quizscore = 0;
        private ILambdaContext context = null;
        private static Random rand = new Random();
        

        /// <summary>
        /// Lambda app entry point  
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public SkillResponse FunctionHandler(SkillRequest input, ILambdaContext ctx)
        {
            context = ctx;
            try
            {
                response = new SkillResponse();
                response.Response = new ResponseBody();
                response.Response.ShouldEndSession = false;
                response.Version = "1.0";
                
                if (input.Request.Type.Equals(AlexaConstants.LaunchRequest))
                {
                    response.Response.OutputSpeech = GetLaunchRequest();
                    appstate = AppState.Start;
                    response.SessionAttributes = new Dictionary<string, object>() { { STATE, appstate.ToString() } };
                }
                else
                {
                    if (input.Request.Type.Equals(AlexaConstants.IntentRequest))
                     {
                        if (IsDialogIntentRequest(input))
                        {
                            if (!IsDialogSequenceComplete(input))
                            { // delegate to Alexa until dialog is complete
                                CreateDelegateResponse();
                                return response;
                            }
                        }

                        response.SessionAttributes = new Dictionary<string, object>();
                        response.Response.OutputSpeech = GetIntentRequest(input);
                        response.SessionAttributes.Add(STATE, appstate.ToString());
                    }
                    else
                    {
                        if (input.Request.Type.Equals(AlexaConstants.SessionEndedRequest) &&
                            string.IsNullOrEmpty(input.Request.Reason) == false)
                        {
                            Log($"session end: " +input.Request.Reason);
                        }
                    }
                }
                Log(JsonConvert.SerializeObject(response));
                return response;
            }
            catch (Exception ex)
            {
                Log($"error :" + ex.Message);
            }

            response.SessionAttributes = new Dictionary<string, object>() { { STATE, appstate.ToString() } };
            (response.Response.OutputSpeech as PlainTextOutputSpeech).Text = HELP_MESSAGE;
            return response;
        }

        /// <summary>
        /// handle the Launch request, return a welcome message
        /// </summary>
        /// <returns>IOutputSpeech innerResponse</returns>
        private IOutputSpeech GetLaunchRequest()
        {
            IOutputSpeech innerResponse = new PlainTextOutputSpeech();
            (innerResponse as PlainTextOutputSpeech).Text = WELCOME_MESSAGE;
            appstate = AppState.Start;
            return innerResponse;
        }

        /// <summary>
        /// Process the Intent requests and return the speeech output
        /// </summary>
        /// <param name="input"></param>
        /// <returns>IOutputSpeech innerResponse</returns>
        private IOutputSpeech GetIntentRequest(SkillRequest input)
        {
            var intentRequest = input.Request;
            IOutputSpeech innerResponse = new PlainTextOutputSpeech();

            MakeItemList();

            switch (intentRequest.Intent.Name)
            {
                case "QuizIntent":
                    DoQuiz(input, innerResponse = new SsmlOutputSpeech());
                    break;

                case "AnswerIntent":
                    appstate = GetAppState(input);
                    Answer(input, innerResponse = new SsmlOutputSpeech());
                    break;

                case "Quiz":
                    DoQuiz(input, innerResponse = new SsmlOutputSpeech());
                    break;
             
                case "AskQuestion":
                    appstate = AppState.Quiz;
                    counter = GetIntAttributeProperty(input.Session.Attributes, COUNTER);
                    AskQuestion(input, innerResponse = new SsmlOutputSpeech());
                    break;

                case AlexaConstants.CancelIntent:
                    (innerResponse as PlainTextOutputSpeech).Text = EXIT_SKILL_MESSAGE;
                    response.Response.ShouldEndSession = true;
                    break;

                case AlexaConstants.StartOverIntent:
                    DoQuiz(input, innerResponse = new SsmlOutputSpeech());
                    response.Response.ShouldEndSession = false;
                    break;

                case AlexaConstants.StopIntent:
                    (innerResponse as PlainTextOutputSpeech).Text = EXIT_SKILL_MESSAGE;
                    response.Response.ShouldEndSession = true;
                    break;

                case AlexaConstants.HelpIntent:
                    (innerResponse as PlainTextOutputSpeech).Text = HELP_MESSAGE;
                    break;

                default:
                    if (appstate == AppState.Quiz)
                    {
                        Answer(input, innerResponse = new SsmlOutputSpeech());
                    }
                    else
                    {
                        (innerResponse as PlainTextOutputSpeech).Text = WELCOME_MESSAGE;
                    }
                    break;
            }

            if (innerResponse.Type == "SSML")
            {
                (innerResponse as SsmlOutputSpeech).Ssml = "<speak>" + (innerResponse as SsmlOutputSpeech).Ssml + "</speak>";
            }

            return innerResponse;
        }

        /// <summary>
        /// Get current state of the app whether its a quiz or fact based
        /// </summary>
        /// <param name="input"></param>
        /// <returns>appstate</returns>
        private AppState GetAppState(SkillRequest input)
        {
            AppState ret = AppState.Start;
            string property = GetStringAttributeProperty(input.Session.Attributes, STATE);
            if (!string.IsNullOrEmpty(property) && property.Equals(AppState.Quiz.ToString()))
            {
                ret = AppState.Quiz;
            }
            return ret;
        }

        /// <summary>
        /// start a quiz
        /// </summary>
        /// <param name="input"></param>
        /// <param name="innerResponse"></param>
        /// <returns>void</returns>
        private void DoQuiz(SkillRequest input, IOutputSpeech innerResponse)
        {
            appstate = AppState.Quiz;
            ClearAppState();
            AskQuestion(input, innerResponse);
        }

        private void ClearAppState()
        {
            counter = 0;
            quizscore = 0;
        }

        /// <summary>
        /// run quiz or facts handler depending on state
        /// </summary>
        /// <param name="input"></param>
        /// <param name="innerResponse"></param>
        /// <returns>void</retur
        private void Answer(SkillRequest input, IOutputSpeech innerResponse)
        {
            if (appstate == AppState.Quiz)
            {
                AnswerQuiz(input, innerResponse);
            }
            else
            {
                AnswerFacts(input, innerResponse);
            }
        }

        /// <summary>
        /// Return the response fact, if user enters a known detail for a state e.g. says "17" find the
        /// information for that state e.g. Ohio, and then return the complete set
        /// </summary>
        /// <param name="input"></param>
        /// <param name="innerResponse"></param>
        /// <returns>void</returns>
        private void AnswerFacts(SkillRequest input, IOutputSpeech innerResponse)
        {
            SsmlOutputSpeech output = (innerResponse as SsmlOutputSpeech);

            string textout = string.Empty;
            var intentRequest = input.Request;
            Item item = GetItem(intentRequest.Intent.Slots, out textout);
            
            if (item != null && item.Capital != null)
            {
                if (this.USE_CARDS_FLAG)
                {
                    StandardCard card = new StandardCard();

                    card.Title = GetCardTitle(item);
                    card.text  = GetTextDescription(item);
                    response.Response.Card = card;

                    card.Image = new CardImage();
                    card.Image.LargeImageUrl = GetLargeImage(item);
                    card.Image.SmallImageUrl = GetSmallImage(item);
                  
                    output.Ssml = GetSpeechDescriptionWithCard(item);
                    response.SessionAttributes.Add(RESPONSE, output.Ssml);
                }
                else
                {
                    output.Ssml = GetSpeechDescription(item);
                    response.SessionAttributes.Add(RESPONSE, output.Ssml);
                }

                SsmlOutputSpeech repromptResponse = new SsmlOutputSpeech();
                repromptResponse.Ssml = DecorateSsml(REPROMPT_SPEECH);
                response.Response.Reprompt = new Reprompt();
                response.Response.Reprompt.OutputSpeech = repromptResponse;
            }
            else
            {
                output.Ssml = GetBadAnswer(textout);
                response.SessionAttributes.Add(RESPONSE, output.Ssml);
                response.Response.Reprompt = new Reprompt();
                response.Response.Reprompt.OutputSpeech = innerResponse;
            }
        }

        /// <summary>
        /// Check the answer returned and determine the correct response
        /// then configure the next question if there is to be one storing the data
        /// inthe sessionAttributes array
        /// </summary>
        /// <param name="input"></param>
        /// <param name="innerReponse"></param>
        /// <returns>void</returns>
        private void  AnswerQuiz(SkillRequest input, IOutputSpeech innerResponse)
        {
            var intentRequest = input.Request;

            Item item  = GetItemAttributeProperty(input.Session.Attributes, QUIZITEM);
            if (item == null)
            {
                GetLaunchRequest();
                return;
            }

            string property = GetStringAttributeProperty(input.Session.Attributes, QUIZPROPERTY);
            quizscore = GetIntAttributeProperty(input.Session.Attributes, QUIZSCORE);
      
            if (CompareSlots(intentRequest.Intent.Slots, item.PropertyValue(property) ) )
            {
                quizscore++;
                (innerResponse as SsmlOutputSpeech).Ssml = GetSpeechCon(true);
            }
            else
            {
                (innerResponse as SsmlOutputSpeech).Ssml = GetSpeechCon(false);
            }

            (innerResponse as SsmlOutputSpeech).Ssml += GetAnswer(property, item);
            if (counter < MAX_QUESTION) 
            {
                (innerResponse as SsmlOutputSpeech).Ssml += GetCurrentScore(quizscore, counter);
                AskQuestion(input, innerResponse);
            }
            else
            {
                (innerResponse as SsmlOutputSpeech).Ssml += GetFinalScore(quizscore, counter);
                (innerResponse as SsmlOutputSpeech).Ssml += " " + EXIT_SKILL_MESSAGE;
                response.SessionAttributes.Add(RESPONSE, (innerResponse as SsmlOutputSpeech).Ssml);
                appstate = AppState.Start;
                ClearAppState();
            }
        }

        /// <summary>
        /// Builds up the details required to ask a question and stores them in the
        /// sessionAtrtibutes array
        /// </summary>
        /// <param name="input"></param>
        /// <param name="innerReponse"></param>
        /// <returns>void</returns>
        private void AskQuestion(SkillRequest input, IOutputSpeech innerResponse)
        {
            if (counter <= 0)
            {
                (innerResponse as SsmlOutputSpeech).Ssml = START_QUIZ_MESSAGE + " ";
                counter = 0;
            }

            counter++;
            response.SessionAttributes.Add(COUNTER, counter);
            Item item = theItems[GetRandomNumber(0, theItems.Count - 1)];

            string property = Item.PropertyNames[GetRandomNumber(0, Item.PropertyNames.Length - 1)];
            response.SessionAttributes.Add(QUIZPROPERTY, property);

            response.SessionAttributes.Add(QUIZITEM, item);
            response.SessionAttributes.Add(QUIZSCORE, quizscore);

            string question = GetQuestion(counter, property, item);
            (innerResponse as SsmlOutputSpeech).Ssml += question;

            response.Response.Reprompt = new Reprompt();
            response.Response.Reprompt.OutputSpeech = new SsmlOutputSpeech();
            (response.Response.Reprompt.OutputSpeech as SsmlOutputSpeech).Ssml = DecorateSsml(question); 
            response.SessionAttributes.Add(RESPONSE, (innerResponse as SsmlOutputSpeech).Ssml);
        }

        static string DecorateSsml(string instr)
        {
            return "<speak>" + instr + "</speak>";
        }

        /// <summary>
        /// Get a named string from the dictionary
        /// </summary>
        /// <param name="property"></param>
        /// <param name="key"></param>
        /// <returns>string</returns>
        string GetStringAttributeProperty (Dictionary <string, object> property, string key) 
        {
            if (property != null)
            {
                if (property.ContainsKey(key))
                {
                    return (string)property[key];
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Get a named Item from the dictionary
        /// </summary>
        /// <param name="property"></param>
        /// <param name="key"></param>
        /// <returns>Item</returns>
        Item GetItemAttributeProperty(Dictionary<string, object> property, string key)
        {
            Item item = null;
            if (property != null)
            {
                if (property.ContainsKey(key))
                {
                    try
                    {
                        var sObj = (Object)property[key];
                        item = JsonConvert.DeserializeObject<Item>((string)sObj.ToString().Replace("\r\n", string.Empty));
                    }
                    catch (Exception ex)
                    {
                        Log("getItemAttributeProperty " + ex.Message);
                    }
                }
            }
            return item;
        }

        /// <summary>
        /// Get a named int property value from the dictionary
        /// </summary>
        /// <param name="property"></param>
        /// <param name="key"></param>
        /// <returns>int</returns>
        int GetIntAttributeProperty(Dictionary<string, object> property, string key)
        {
            if (property != null)
            {
                if (property.ContainsKey(key))
                {
                    try
                    {
                        Int64 i = (Int64)property[key];
                        return (int) i;
                    }
                    catch (Exception ex)
                    {
                        Log("getIntAttributeProperty " + ex.Message);
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// Returns a descriptive sentence about your data. Before a user starts a quiz, they can ask about a
        /// specific data element, like "Ohio." The skill will speak the sentence from this function, 
        /// pulling the data values from the appropriate record in your data.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>string</returns>
        private string GetSpeechDescriptionWithCard(Item item)
        {
            return item.StateName + " is the " + item.StatehoodOrder + "th state, admitted to the Union in " + item.StatehoodYear +
                ". The capital of " + item.StateName + " is " + item.Capital + ", and the abbreviation for " + item.StateName +
                " is " + breakstrong + sayas_spellout + item.Abbreviation + sayas +".  I've added " + item.StateName 
                + " to your Alexa app.  Which other state or capital would you like to know about?";
        }

        /// <summary>
        /// Returns a descriptive sentence about your data. Before a user starts a quiz, they can ask about
        /// a specific data element,like "Ohio."  The skill will speak the sentence from this function, 
        /// pulling the data values from the appropriate record in your data.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>string</returns>
        private string GetSpeechDescription(Item item)
        {
            //   return item.StateName + " is the " + item.StatehoodOrder + "th state, admitted to the Union in " + item.StatehoodYear +
            //     ". The capital of " + item.StateName + " is " + item.Capital + ", and the abbreviation for " + item.StateName +
            //   " is " + breakstrong + sayas_spellout + item.Abbreviation + sayas + ". Which other state or capital would you like to know about?";

            return item.StateName + " is the " + item.StatehoodOrder + "th state, admitted to the Union in " + item.StatehoodYear +
           ". The capital of " + item.StateName + " is " + item.Capital + ", and the abbreviation for " + item.StateName +
           " is " + item.Abbreviation +". Which other state or capital would you like to know about?";

        }

        /// <summary>
        /// We have provided two ways to create your quiz questions.  The default way is to phrase all of your
        /// questions like: "What is X of Y?" If this approach doesn't work for your data, take a look at 
        /// the commented code in this function.  You can write a different question structure for each
        /// property of your data.
        /// </summary>
        /// <param name="counter"></param>
        /// <param name="property"></param>
        /// <param name="item"></param>
        /// <returns>string</returns>
        private string GetQuestion(int counter, string property, Item item)
        {
            return "Here is your " + counter.ToString() + "th question.  What is the " + property + " of " + item.StateName + "?";
            /*
            switch(property)
            {
                case "City":
                    return "Here is your " + counter + "th question.  In what city do the " + item.League + "'s "  + item.Mascot + " play?";
                break;
                case "Sport":
                    return "Here is your " + counter + "th question.  What sport do the " + item.City + " " + item.Mascot + " play?";
                break;
                case "HeadCoach":
                    return "Here is your " + counter + "th question.  Who is the head coach of the " + item.City + " " + item.Mascot + "?";
                break;
                default:
                    return "Here is your " + counter + "th question.  What is the " + property + " of the "  + item.Mascot + "?";
                break;
            }
            */
        }
        /// <summary>
        ///Returns an answer during the quiz. Much like the "getQuestion" function above, you can use a
        ///switch() statement to create different responses for each property in your data.  For example,
        ///when this quiz has an answer that includes a state abbreviation, we add some SSML to make
        ///sure that Alexa spells that abbreviation out (instead of trying to pronounce it.)
        /// </summary>
        /// <param name="property"></param>
        /// <param name="item"></param>
        /// <returns>string</returns>
        private string GetAnswer(string property, Item item)
        {
            string ret = string.Empty;
            switch (property)
            {
                case "Abbreviation": ret = "The " + property + " of " + item.StateName + " is " + sayas_spellout +  item.PropertyValue(property) + sayas +". ";
                                     break;
                default: ret = "The " + property + " of " + item.StateName + " is " + item.PropertyValue(property) + ". ";
                         break;
            }
            return ret;
        }
        
        /// <summary>
        ///  compare the value of the answer returned with the value stored in
        ///  the slot, Return true if the values match e.g. correct answer or
        ///  false if not, incorrect answer
        /// </summary>
        /// <param name="slots"></param>
        /// <param name="svalue"></param>
        /// <returns>bool</returns>
        private bool CompareSlots(Dictionary<string, Slot> slots, string svalue)
        {
            string val = svalue.ToLower();
            foreach (KeyValuePair<string, Slot> kvp in slots)
            {
                if (!string.IsNullOrEmpty(kvp.Value.Value))
                {
                    string kval = kvp.Value.Value.ToString().ToLower();
                    if (kval.Equals(val))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Get a named item from the slots 
        /// </summary>
        /// <param name="slots"></param>
        /// <param name="out text"></param>
        /// <returns>Item</returns>
        private Item GetItem(Dictionary<string, Slot> slots, out string valueName)
        {
            valueName = string.Empty;
            try
            {
                string[] properties = Item.PropertyNames;

                foreach (KeyValuePair<string, Slot> kvp in slots)
                {
                    if (!string.IsNullOrEmpty(kvp.Value.Value))
                    {
                        string val = kvp.Value.Value.ToLower();
                        valueName = kvp.Value.Value;
                        foreach (string sprop in properties)
                        {
                            var item = theItems.FindAll(x => x.PropertyValue(sprop).ToLower().Equals(val)).FirstOrDefault();
                            if (item != null)
                            {
                                return item;
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Log("GetItem "+ex.Message);
            }
            return null;
        }

        /// <summary>
        ///This is the response a user will receive when they ask about something we weren't expecting. 
        ///For example, say "pizza" to your skill when it starts. This is the response you will receive.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>string</returns>
        private string GetBadAnswer(string item)
        {
            if (string.IsNullOrEmpty(item))
            {
                item = "This";
            }
            return "I'm sorry. " + item + " is not something I know very much about in this skill. " + HELP_MESSAGE;
        }

        /// <summary>
        ///  create a delegate response, we delegate all the dialog requests
        ///  except "Complete"
        /// </summary>
        /// <returns>void</returns>
        private void CreateDelegateResponse()
        {
            DialogDirective dld = new DialogDirective()
            {
                Type = AlexaConstants.DialogDelegate
            };
            response.Response.Directives.Add(dld);
        }

        /// <summary>
        /// Check if its IsDialogIntentRequest, e.g. part of a Dialog sequence
        /// </summary>
        /// <param name="input"></param>
        /// <returns>bool true if a dialog</returns>
        private bool IsDialogIntentRequest(SkillRequest input)
        {
            if (string.IsNullOrEmpty(input.Request.DialogState))
                return false;
            return true;
        }

        /// <summary>
        /// Check if its Dialog sequence is complete e.g AlexaConstants.DialogCompleted is true
        /// </summary>
        /// <param name="input"></param>
        /// <returns>bool true if dialog complete set</returns>
        private bool IsDialogSequenceComplete(SkillRequest input)
        {
           if (input.Request.DialogState.Equals(AlexaConstants.DialogCompleted))
           {
              return true;
           }
           return false;
        }


        private int GetRandomNumber(int min, int max)
        {
            return (int)Math.Floor((decimal)(rand.NextDouble() * (double)((max - min + 1) + min)));
        }

        private string GetTextDescription(Item item)
        {
            return Item.GetFormatedText(item);
        }

        private string GetRandomSymbolSpeech(string symbol)
        {
            return sayas_spellout + symbol + sayas;
        }

        private string GetSpeechCon(bool type)
        {
            if (type)
            {
                return sayas_interject + speechConsCorrect[GetRandomNumber(0, speechConsCorrect.Length - 1)] + "! " + sayas + breakstrong;
            }
            return sayas_interject + speechConsWrong[GetRandomNumber(0, speechConsWrong.Length - 1)] + " " + sayas + breakstrong;
        }
        
        //This is the message a user will receive after each question of a quiz. It reminds them of their current score.
        private string GetCurrentScore(int score, int counter) { return "Your current score is " + score.ToString() + " out of " + counter.ToString() + ". "; }

        //This is the message a user will receive after they complete a quiz.It tells them their final score.
        private string GetFinalScore(int score, int counter) { return "Your final score is " + score.ToString() + " out of " + counter.ToString() + ". "; }

        //This is what your card title will be. For our example, we use the name of the state the user requested.
        private string GetCardTitle(Item item) { return item.StateName; }

        //This is the small version of the card image.  We use our data as the naming convention for our 
        //images so that we can dynamically generate the URL to the image.
        //The small image should be 720x400 in dimension.
        private string GetSmallImage(Item item)
        { return "https://m.media-amazon.com/images/G/01/mobile-apps/dex/alexa/alexa-skills-kit/tutorials/quiz-game/state_flag/720x400/" 
        + item.Abbreviation + "._TTH_.png"; }

        //This is the large version of the card image.  It should be 1200x800 pixels in dimension.
        private string GetLargeImage(Item item)
        { return "https://m.media-amazon.com/images/G/01/mobile-apps/dex/alexa/alexa-skills-kit/tutorials/quiz-game/state_flag/1200x800/"
                + item.Abbreviation + "._TTH_.png"; }

        //This is a list of positive speechcons that this skill will use when a user gets a correct answer.
        //For a full list of supported speechcons, go here:
        //https://developer.amazon.com/public/solutions/alexa/alexa-skills-kit/docs/speechcon-reference
        string[] speechConsCorrect = new string[] {"Booya", "All righty", "Bam", "Bazinga", "Bingo", "Boom",
        "Bravo", "Cha Ching", "Cheers", "Dynomite", "Hip hip hooray", "Hurrah", "Hurray", "Huzzah",
        "Oh dear.  Just kidding.  Hurray", "Kaboom", "Kaching", "Oh snap", "Phew",
        "Righto", "Way to go", "Well done", "Whee", "Woo hoo", "Yay", "Wowza", "Yowsa"};

        //This is a list of negative speechcons that this skill will use when a user gets an incorrect answer.
        //For a full list of supported speechcons, go here: 
        //https://developer.amazon.com/public/solutions/alexa/alexa-skills-kit/docs/speechcon-reference
        string[] speechConsWrong = new string[] {"Argh", "Aw man", "Blarg", "Blast", "Boo", "Bummer", "Darn",
        "D'oh", "Dun dun dun", "Eek", "Honk", "Le sigh", "Mamma mia", "Oh boy", "Oh dear", "Oof", "Ouch",
        "Ruh roh", "Shucks", "Uh oh", "Wah wah", "Whoops a daisy", "Yikes" };


        //This is the message a user will hear when they start a quiz.
        string START_QUIZ_MESSAGE = "OK.  I will ask you " + MAX_QUESTION.ToString() + " questions about the United States.";
        
        /// <summary>
        /// Create the list of USA States with details  
        /// </summary>
        /// <returns></returns>
        private void MakeItemList()
        {
            if (theItems == null)
            {
                theItems = Item.ItemsArray();
            }
        }

        /// <summary>
        /// logger interface
        /// </summary>
        /// <param name="text"></param>
        /// <returns>void</returns>
        private void Log(string text)
        {
            if (context != null)
            {
                context.Logger.LogLine(text);
            }
        }
    }
}
