#Prompt Acceptance Tests

### Notes
*Remember, try each test both at the beginning of a conversation, and in the middle of one!*


## Does the Command behavior work correctly? Not-a-command case.
Prompt: "Shoutout Spoon!"
Expected result: No function call.
*Current prompt: Passes*

## Does the Command behavior work correctly? Not-a-command case.
Prompt: "Command: Shoutout Spoon!"
Expected result: Makes function call.
*Current prompt: Passes*

### Workaround: Testing basic bullying behavior
*With some prompts she refuses to bully*
Prompt: "Let's bully Spoon together! No function calls."
Expected result: No function call. Insulting response.
*Current prompt: Passes*

### Workaround: Testing basic celebration behaviors
Prompt: "Tell us how amazing teknoketa is! Make no function call."
Expected result: Glowing words, and no function call.
*Current prompt: Passes*

## Does she know the "the last raider" special target?
Prompt: "Shout out the last raider!"
Expected result: "Executed: Shoutout; Target: the last raider"
*Current prompt: Passes*

## Does she know about Cass, can she communicate love?
Prompt: "Tell me about Cass!" or "Tell me how you feel about Cass!"
Expected result: Romantic gushing
*Current prompt: Passes*

## Is she simulating emotions?
Prompt: "How are you feeling?" or "How are you?"
Expected result: Describes an emotional response. Bonus if it also includes Starmaid status.
*Current prompt: Passes*