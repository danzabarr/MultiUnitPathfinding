using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[ExecuteInEditMode]
public class DialogueBox : MonoBehaviour
{
    /// <summary>
    /// The text display component that will be filled with text.
    /// </summary>
    [SerializeField] TextMeshProUGUI display;

    /// <summary>
    /// The object to display when the text is finished.
    /// </summary>
    [SerializeField] GameObject promptIndicator;

    /// <summary>
    /// The text buffer to display.
    /// </summary>
    [TextArea(3, 10)]
    [SerializeField] string buffer = "";

    /// <summary>
    /// Tracks whether the text is currently prompting for input.
    /// Not used internally.
    /// </summary>
    [SerializeField] bool prompting = false;

    /// <summary>
    /// Pages are calculated by splitting the text by newlines,
    /// and then by how many words can fit on a page.
    /// We calculate the pages first by adding words to a page until we reach the limit,
    /// then we add the page to the list of pages and start a new page.
    /// </summary>
    [SerializeField] List<string> pages = new List<string>();

    /// <summary>
    /// The page in the process of being displayed.
    /// Once the coroutine has finished, this will be null, 
    /// and calling continue will pop the next page.
    /// </summary>
    [SerializeField] string page = null;

    /// <summary>
    /// Reference to the coroutine that is currently displaying text.
    /// </summary>
    [SerializeField] Coroutine routine;
    
    internal Action onFinish;

    /// <summary>
    /// A buffer of text, which will be split into pages.
    /// Changing this value will cause the pages to be recalculated, and the text display to be reset.
    /// Always returns the original text you set.
    /// Doesn't begin displaying the text until you call Continue()
    /// </summary>
    public string Buffer 
    {
        get => buffer;
        set
        {
            buffer = value ?? "";
            Reset();
        }
    }

    /// <summary>
    /// This is the currently displayed text as a string.
    /// Setting this value will immediately display the text
    /// and won't affect the buffer.
    /// It returns whatever is currently displayed.
    /// </summary>
    public string DisplayedText
    {
        get => display != null ? display.text : "";
        private set
        {
            if (display == null)
                return;
            display.text = value ?? "";
            display.ForceMeshUpdate();
        }
    }

    /// <summary>
    /// Whether the text is currently prompting for input.
    /// </summary>
    public bool Prompting 
    {
        get => prompting;
        private set
        {
            prompting = value;
            if (promptIndicator != null)
                promptIndicator.SetActive(value);
        }
    }

    /// <summary>
    /// Whether the dialogue box has finished displaying all of the buffer.
    /// Set this true to hide the dialogue box and call the onFinish action.
    /// This gets called automatically when there are no more pages to display.
    /// </summary>
    public bool Complete 
    {
        get => page == null && pages.Count == 0;
        private set 
        {
            if (value)
            {
                pages.Clear();
                page = null;
                DisplayedText = "";
                Prompting = false;
                gameObject.SetActive(false);
                onFinish?.Invoke();
            }
        }
    }
    
    /// <summary>
    /// Displays the text in the dialogue box.
    /// Sets the buffer to the text and resets the dialogue box.
    /// Then starts displaying the text.
    /// </summary>
    /// <param name="text"></param>
    public void Show(string text, Action onFinish = null)
    {
        this.onFinish = onFinish;
        DisplayedText = "";
        Buffer = text;
        gameObject.SetActive(true);
        Continue();
    }

    public void Hide() => gameObject.SetActive(false);

    void OnValidate() => Reset();
    
    void Start() => Reset();

    void Update()
    {
        if (Input.GetButtonDown("Fire1"))
            Continue();
    }

    private IEnumerator FillDisplay(float delay = 0.025f)
    {
        DisplayedText = "";

        string[] words = page.Split(' ');
        foreach (string word in words)
        {
            DisplayedText += word + " ";
            yield return new WaitForSeconds(delay);
        }

        Prompting = true;
        page = null;
        routine = null;
    }

    /// <summary>
    /// If text is currently being displayed, pushing continue will immediately display the rest of the text.
    /// If the current page of text has finished being displayed, tries to pop the next page of text and start displaying it.
    /// If there are no more pages, does nothing.
    /// </summary>
    [ContextMenu("Continue")]
    public void Continue()
    {
        // If we're currently displaying text, stop the coroutine.
        if (routine != null) 
            StopCoroutine(routine);
        routine = null;
        
        // If we were printing, display the rest of the text.
        if (page != null)
        {
            DisplayedText = page;
            Prompting = true;
        }

        else
        {
            // Clear the display and pop a new page if possible.
            DisplayedText = "";
            Prompting = false;
        
            if (pages.Count > 0)
            {
                page = pages[0];
                pages.RemoveAt(0);
                routine = StartCoroutine(FillDisplay(0.1f));
                return; // and skip page = null
            }
        }

        // If we're out of pages, hide the dialogue box.
        page = null;
        if (pages.Count == 0)
            Complete = true;
        
    }

    /// <summary>
    /// Resets the dialogue box to the beginning of the buffer.
    /// </summary>
    [ContextMenu("Reset")]
    public void Reset()
    {
        pages.Clear();
        page = null;
        Prompting = false;
        
        if (string.IsNullOrEmpty(Buffer))
            return;

        string[] paragraphs = Buffer.Split('\n');
        foreach (string paragraph in paragraphs)
        {
            string[] words = paragraph.Split(' ');
            string page = ""; // local hiding
            DisplayedText = "";

            foreach (string word in words)
            {
                DisplayedText += word + " ";

                if (display.isTextOverflowing)
                {
                    pages.Add(page);
                    page = "";
                    DisplayedText = "";
                }

                page += word + " ";
            }
            
            if (page != "")
                pages.Add(page);
        }

        DisplayedText = "";
    }
}
