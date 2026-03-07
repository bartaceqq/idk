using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class VisualCommunication : MonoBehaviour
{
    public TMP_Text text;
    public NPCText npctext;
    public float letterDelay = 0.03f;
    public float sentenceDelay = 1.5f;
    public bool clearBeforeEachSentence = true;

    public static bool IsTalking { get; private set; }

    private Coroutine typingRoutine;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (text != null)
        {
            text.enabled = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartAddingWords()
    {
        if (text == null || npctext == null || npctext.texts == null)
        {
            return;
        }

        text.enabled = true;
        IsTalking = true;
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
        }

        typingRoutine = StartCoroutine(AddAllTexts());
    }

    private IEnumerator AddAllTexts()
    {
        if (text == null || npctext == null || npctext.texts == null)
        {
            IsTalking = false;
            yield break;
        }

        if (clearBeforeEachSentence)
        {
            text.text = string.Empty;
        }

        for (int i = 0; i < npctext.texts.Count; i++)
        {
            string sentence = npctext.texts[i] ?? string.Empty;
            yield return StartCoroutine(AddSentenceLetters(sentence, i));

            // Keep each finished sentence visible before moving to the next one.
            if (sentenceDelay > 0f)
            {
                yield return new WaitForSeconds(sentenceDelay);
            }
        }

        typingRoutine = null;
        IsTalking = false;
        text.enabled = false;
    }

    public IEnumerator AddSentenceLetters(string sentence, int counter)
    {
        if (text == null)
        {
            yield break;
        }

        if (clearBeforeEachSentence)
        {
            text.text = string.Empty;
        }
        else if (counter > 0)
        {
            text.text += "\n";
        }

        if (string.IsNullOrEmpty(sentence))
        {
            yield break;
        }

        for (int i = 0; i < sentence.Length; i++)
        {
            text.text += sentence[i];

            if (i < sentence.Length - 1 && letterDelay > 0f)
            {
                yield return new WaitForSeconds(letterDelay);
            }
        }
    }

    private void OnDisable()
    {
        StopTyping();
    }

    private void OnDestroy()
    {
        StopTyping();
    }

    private void StopTyping()
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        IsTalking = false;

        if (text != null)
        {
            text.enabled = false;
        }
    }
}
