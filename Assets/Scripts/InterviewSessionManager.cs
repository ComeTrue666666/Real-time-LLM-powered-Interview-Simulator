using UnityEngine;
using TMPro;

public class InterviewSessionManager : MonoBehaviour
{

    [Header("Interview Runtime Data")]
    public int currentQuestionIndex = 0;
    public int maxQuestions = 10;

    private string interviewHistory = "";
    private string currentQuestion = "";
    public enum InterviewState
    {
        Setup,
        InterviewActive,
        Paused,
        Finished
    }

    [Header("Current Interview Settings")]
    public string selectedJobCategory = "AI";
    public string selectedDifficulty = "Medium";
    public string selectedPersonality = "Normal";

    [Header("Current State")]
    public InterviewState currentState = InterviewState.Setup;

    [Header("UI References")]
    public GameObject setupPanel;
    public GameObject questionPanel;
    public TMP_Text questionText;
    public TMP_Text statusText;
    public TMP_InputField answerInputField;

    [Header("Interview Control Buttons")]
    public GameObject restartInterviewButton;
    public GameObject pauseResumeButton;
    public TMP_Text pauseResumeButtonText;
    public GameObject endInterviewButton;

    [Header("Voice Recording Buttons")]
    public GameObject recordAnswerButton;
    public GameObject stopRecordingButton;

    [Header("LLM Service")]
    public LLMInterviewService llmService;

    [Header("Text To Speech")]
    public TextToSpeechPlayer textToSpeechPlayer;

    private bool isWaitingForLLM = false;
    private bool isPaused = false;

    private void Start()
    {
        EnterSetupMode();
    }

    public void SetJobCategory(string category)
    {
        selectedJobCategory = category;
        UpdateStatusText();
    }

    public void SetDifficulty(string difficulty)
    {
        selectedDifficulty = difficulty;
        UpdateStatusText();
    }

    public void SetPersonality(string personality)
    {
        selectedPersonality = personality;
        UpdateStatusText();
    }

    public void StartInterview()
{
    currentState = InterviewState.InterviewActive;
    isPaused = false;
    isWaitingForLLM = false;

    Debug.Log("StartInterview clicked.");

    if (setupPanel != null)
        setupPanel.SetActive(false);
    else
        Debug.LogError("setupPanel is not assigned!");

    if (questionPanel != null)
        questionPanel.SetActive(true);
    else
        Debug.LogError("questionPanel is not assigned!");
    
    if (restartInterviewButton != null)
        restartInterviewButton.SetActive(false);

    if (pauseResumeButton != null)
        pauseResumeButton.SetActive(true);

    if (endInterviewButton != null)
        endInterviewButton.SetActive(true);

    if (pauseResumeButtonText != null)
        pauseResumeButtonText.text = "Pause";
    
    SetVoiceRecordingButtonsVisible(true);

    currentQuestionIndex = 0;
    interviewHistory = "";

    if (questionText != null)
    {
        questionText.text =
            "Interview started.\n\n" +
            "Job Category: " + selectedJobCategory + "\n" +
            "Difficulty: " + selectedDifficulty + "\n" +
            "Interviewer Style: " + selectedPersonality + "\n\n" +
            "Interviewer is thinking...";
    }

    if (llmService == null)
    {
        Debug.LogError("LLM Service is not assigned!");
        return;
    }

    isWaitingForLLM = true;

    StartCoroutine(llmService.GenerateFirstQuestion(
        selectedJobCategory,
        selectedDifficulty,
        selectedPersonality,
        OnFirstQuestionReady
    ));
}


    private void OnFirstQuestionReady(string question)
{
        isWaitingForLLM = false;

        currentQuestionIndex++;
        currentQuestion = question;

        if (questionText != null)
        {
            questionText.text = currentQuestion;
        }

        if (textToSpeechPlayer != null)
        {
            textToSpeechPlayer.Speak(currentQuestion);
        }
        else
        {
            Debug.LogWarning("TextToSpeechPlayer is not assigned.");
        }

        Debug.Log("First LLM question ready: " + currentQuestion);
    }

    public void EnterSetupMode()
    {
        currentState = InterviewState.Setup;
        isPaused = false;
        isWaitingForLLM = false;

        currentQuestionIndex = 0;
        interviewHistory = "";
        currentQuestion = "";

        if (setupPanel != null)
            setupPanel.SetActive(true);

        if (questionPanel != null)
            questionPanel.SetActive(false);

        if (restartInterviewButton != null)
            restartInterviewButton.SetActive(false);

        if (pauseResumeButton != null)
            pauseResumeButton.SetActive(false);

        if (endInterviewButton != null)
            endInterviewButton.SetActive(false);

        SetVoiceRecordingButtonsVisible(false);

        if (pauseResumeButtonText != null)
            pauseResumeButtonText.text = "Pause";

        UpdateStatusText();

        Debug.Log("Entered setup mode.");
    }

    private void UpdateStatusText()
    {
        if (statusText == null)
            return;

        statusText.text =
            "Selected Settings:\n" +
            "Job: " + selectedJobCategory + "\n" +
            "Difficulty: " + selectedDifficulty + "\n" +
            "Style: " + selectedPersonality;
    }


    private string GenerateMockQuestion()
    {
        currentQuestionIndex++;

        if (currentQuestionIndex == 1)
        {
            return "Tell me about yourself and why you are interested in this " 
                + selectedJobCategory + " role.";
        }

        if (selectedJobCategory == "AI")
        {
            return "Can you explain what overfitting means in machine learning?";
        }
        else if (selectedJobCategory == "Data Science")
        {
            return "How would you handle missing values in a dataset?";
        }
        else if (selectedJobCategory == "Software")
        {
            return "Can you explain the difference between an array and a linked list?";
        }

        return "Can you explain one technical concept related to this role?";
    }


    public void SubmitVoiceAnswer(string userAnswer)
    {
        if (!CanAcceptVoiceAnswer())
        {
            Debug.LogWarning("Cannot submit voice answer right now. Interview may be paused or waiting for LLM.");
            return;
        }

        if (isWaitingForLLM)
        {
            Debug.LogWarning("Still waiting for LLM response.");
            return;
        }

        userAnswer = userAnswer.Trim();

        if (string.IsNullOrEmpty(userAnswer))
        {
            if (questionText != null)
            {
                questionText.text =
                    "I could not hear a clear answer. Please try recording again.\n\n" +
                    "Question " + currentQuestionIndex + ":\n" +
                    currentQuestion;
            }

            Debug.LogWarning("Voice transcript was empty.");
            return;
        }

        interviewHistory += "\nInterviewer: " + currentQuestion;
        interviewHistory += "\nUser: " + userAnswer;

        if (answerInputField != null)
        {
            answerInputField.text = "";
        }

        if (currentQuestionIndex >= maxQuestions)
        {
            StartLLMFinalFeedback();
            return;
        }

        if (questionText != null)
        {
            questionText.text =
                "Answer received.\n\n" +
                "Interviewer is thinking...";
        }

        if (llmService == null)
        {
            Debug.LogError("LLM Service is not assigned!");
            return;
        }

        isWaitingForLLM = true;

        StartCoroutine(llmService.GenerateFollowUpQuestion(
            selectedJobCategory,
            selectedDifficulty,
            selectedPersonality,
            interviewHistory,
            userAnswer,
            OnFollowUpQuestionReady
        ));

        Debug.Log("Voice answer submitted directly to LLM: " + userAnswer);
    }

    public void SubmitAnswer()
{
    if (currentState != InterviewState.InterviewActive)
        return;

    if (isWaitingForLLM)
    {
        Debug.LogWarning("Still waiting for LLM response.");
        return;
    }

    if (answerInputField == null)
    {
        Debug.LogError("answerInputField is not assigned!");
        return;
    }

    string userAnswer = answerInputField.text.Trim();

    if (string.IsNullOrEmpty(userAnswer))
    {
        if (questionText != null)
        {
            questionText.text =
                "Please enter an answer before submitting.\n\n" +
                "Question " + currentQuestionIndex + ":\n" +
                currentQuestion;
        }

        Debug.LogWarning("User tried to submit an empty answer.");
        return;
    }

    interviewHistory += "\nInterviewer: " + currentQuestion;
    interviewHistory += "\nUser: " + userAnswer;

    answerInputField.text = "";

    if (currentQuestionIndex >= maxQuestions)
    {
        StartLLMFinalFeedback();
        return;
    }

    if (questionText != null)
    {
        questionText.text =
            "Your answer was submitted.\n\n" +
            "Interviewer is thinking...";
    }

    if (llmService == null)
    {
        Debug.LogError("LLM Service is not assigned!");
        return;
    }

    isWaitingForLLM = true;

    StartCoroutine(llmService.GenerateFollowUpQuestion(
        selectedJobCategory,
        selectedDifficulty,
        selectedPersonality,
        interviewHistory,
        userAnswer,
        OnFollowUpQuestionReady
    ));
}


    private void OnFollowUpQuestionReady(string question)
    {
        isWaitingForLLM = false;

        currentQuestionIndex++;
        currentQuestion = question;

        if (questionText != null)
        {
            questionText.text = currentQuestion;
        }

        if (textToSpeechPlayer != null)
        {
            textToSpeechPlayer.Speak(currentQuestion);
        }
        else
        {
            Debug.LogWarning("TextToSpeechPlayer is not assigned.");
        }

        Debug.Log("Follow-up LLM question ready: " + currentQuestion);
    }


    public void FinishInterview()
    {
        currentState = InterviewState.Finished;

        SetVoiceRecordingButtonsVisible(false);

        if (questionText != null)
        {
            questionText.text =
                "Interview finished.\n\n" +
                "Job Category: " + selectedJobCategory + "\n" +
                "Difficulty: " + selectedDifficulty + "\n" +
                "Interviewer Style: " + selectedPersonality + "\n\n" +
                "Mock Feedback:\n" +
                "You completed the interview. Later, this section will be generated by the LLM.";
        }

        Debug.Log("Interview finished.");
    }


    public void SubmitPresetAnswer(string userAnswer)
{
    if (currentState != InterviewState.InterviewActive)
        return;

    if (isWaitingForLLM)
    {
        Debug.LogWarning("Still waiting for LLM response.");
        return;
    }

    if (string.IsNullOrEmpty(userAnswer))
    {
        Debug.LogWarning("Preset answer is empty.");
        return;
    }

    interviewHistory += "\nInterviewer: " + currentQuestion;
    interviewHistory += "\nUser: " + userAnswer;

    if (currentQuestionIndex >= maxQuestions)
    {
        StartLLMFinalFeedback();
        return;
    }

    if (questionText != null)
    {
        questionText.text =
            "Preset answer submitted:\n" +
            userAnswer + "\n\n" +
            "Interviewer is thinking...";
    }

    if (llmService == null)
    {
        Debug.LogError("LLM Service is not assigned!");
        return;
    }

    isWaitingForLLM = true;

    StartCoroutine(llmService.GenerateFollowUpQuestion(
        selectedJobCategory,
        selectedDifficulty,
        selectedPersonality,
        interviewHistory,
        userAnswer,
        OnFollowUpQuestionReady
    ));

    Debug.Log("Preset answer submitted: " + userAnswer);
}


    public void EndInterviewEarlyAndGetFeedback()
    {
        if (currentState != InterviewState.InterviewActive &&
            currentState != InterviewState.Paused)
        {
            Debug.LogWarning("Cannot end interview early because interview is not active or paused.");
            return;
        }

        if (isWaitingForLLM)
        {
            Debug.LogWarning("Cannot end interview while waiting for LLM response.");

            if (questionText != null)
            {
                questionText.text =
                    "Please wait for the interviewer to finish responding before ending the interview.";
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(interviewHistory))
        {
            Debug.LogWarning("Cannot generate feedback because there is no answered conversation yet.");

            if (questionText != null)
            {
                questionText.text =
                    "You need to answer at least one question before getting interview feedback.\n\n" +
                    "Current question:\n" +
                    currentQuestion;
            }

            return;
        }

        Debug.Log("User ended interview early. Generating final feedback.");

        StartLLMFinalFeedback();
    }

    private void StartLLMFinalFeedback()
{
    currentState = InterviewState.Finished;
    isPaused = false;

    SetVoiceRecordingButtonsVisible(false);

    if (endInterviewButton != null)
    {
        endInterviewButton.SetActive(false);
    }

    if (pauseResumeButton != null)
    {
        pauseResumeButton.SetActive(false);
    }

    if (questionText != null)
    {
        questionText.text =
            "Interview finished.\n\n" +
            "Generating final feedback...";
    }

    if (llmService == null)
    {
        Debug.LogError("LLM Service is not assigned!");
        return;
    }

    isWaitingForLLM = true;

    StartCoroutine(llmService.GenerateFinalFeedback(
        selectedJobCategory,
        selectedDifficulty,
        selectedPersonality,
        interviewHistory,
        OnFinalFeedbackReady
    ));
}


    private void OnFinalFeedbackReady(string feedback)
    {
        isWaitingForLLM = false;
        isPaused = false;
        currentState = InterviewState.Finished;

        SetVoiceRecordingButtonsVisible(false);

        if (questionText != null)
        {
            questionText.text =
                feedback +
                "\n\nInterview complete.\n" +
                "You may restart and choose a new category, difficulty, or interviewer style.";
        }

        if (restartInterviewButton != null)
        {
            restartInterviewButton.SetActive(true);
        }

        if (pauseResumeButton != null)
        {
            pauseResumeButton.SetActive(false);
        }

        if (endInterviewButton != null)
        {
            endInterviewButton.SetActive(false);
        }

        if (textToSpeechPlayer != null)
        {
            textToSpeechPlayer.Speak(feedback);
        }
        else
        {
            Debug.LogWarning("TextToSpeechPlayer is not assigned.");
        }

        Debug.Log("Final LLM feedback ready.");
    }


    public void RestartToSetup()
    {
        Debug.Log("Restart button clicked. Returning to setup mode.");

        if (textToSpeechPlayer != null && textToSpeechPlayer.interviewerAudioSource != null)
        {
            textToSpeechPlayer.interviewerAudioSource.Stop();
        }

        EnterSetupMode();
    }

    public void TogglePauseResume()
    {
        if (currentState == InterviewState.InterviewActive)
        {
            PauseInterview();
        }
        else if (currentState == InterviewState.Paused)
        {
            ResumeInterview();
        }
    }

    public void PauseInterview()
    {
        if (currentState != InterviewState.InterviewActive)
            return;

        currentState = InterviewState.Paused;
        isPaused = true;

        if (textToSpeechPlayer != null && textToSpeechPlayer.interviewerAudioSource != null)
        {
            textToSpeechPlayer.interviewerAudioSource.Pause();
        }

        if (pauseResumeButtonText != null)
        {
            pauseResumeButtonText.text = "Resume";
        }

        if (questionText != null)
        {
            questionText.text =
                "Interview paused.\n\n" +
                "Current question:\n" +
                currentQuestion + "\n\n" +
                "Click Resume when you are ready to continue.";
        }

        Debug.Log("Interview paused.");
    }

    public void ResumeInterview()
    {
        if (currentState != InterviewState.Paused)
            return;

        currentState = InterviewState.InterviewActive;
        isPaused = false;

        if (textToSpeechPlayer != null && textToSpeechPlayer.interviewerAudioSource != null)
        {
            textToSpeechPlayer.interviewerAudioSource.UnPause();
        }

        if (pauseResumeButtonText != null)
        {
            pauseResumeButtonText.text = "Pause";
        }

        if (questionText != null)
        {
            questionText.text =
                "Question " + currentQuestionIndex + ":\n" +
                currentQuestion;
        }

        Debug.Log("Interview resumed.");
    }


    private void SetVoiceRecordingButtonsVisible(bool visible)
    {
        if (recordAnswerButton != null)
        {
            recordAnswerButton.SetActive(visible);
        }

        if (stopRecordingButton != null)
        {
            stopRecordingButton.SetActive(visible);
        }
    }

    public bool CanAcceptVoiceAnswer()
    {
        return currentState == InterviewState.InterviewActive && !isPaused && !isWaitingForLLM;
    }

}