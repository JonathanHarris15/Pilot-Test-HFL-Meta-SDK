using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System;

public class whackamole_manager : MonoBehaviour
{
    [Header("Game Objects")]
    [Tooltip("Index 0 MUST be your Start Button. The rest are moles.")]
    public List<GameObject> button_list;
    public TextMeshPro timer_object;

    [Tooltip("The parent object that will slide along the room's X axis.")]
    public GameObject hand;

    [Tooltip("Drag your Environment/Room object here so the hand knows which way 'Right' is.")]
    public Transform environment;

    [Header("Game Settings")]
    public int n = 10; // Number of times a mole is pressed

    [Tooltip("The final distance the hand will slide relative to the room.")]
    public float final_hand_offset = 500f;
    [Tooltip("How long (in seconds) it takes the hand to reach the final offset.")]
    public float hand_offset_time = 5f;

    [Header("Game Variables")]
    private float elapsed_time = 0f;
    private float best_score = float.MaxValue;
    private int press_count = 0;
    private bool is_timer_running = false;
    private bool is_counting_down = false;

    // We store the hand's starting world position here so we slide relative to it
    private Vector3 initial_hand_pos;
    private float hand_timer = 0f;

    void Start()
    {
        // Memorize exactly where the hand offset wrapper is placed in the world
        if (hand != null)
        {
            initial_hand_pos = hand.transform.position;
        }

        ResetGame();
    }

    void Update()
    {
        if (is_timer_running)
        {
            // Update game timer
            elapsed_time += Time.deltaTime;
            UpdateTimerDisplay();

            // Handle hand movement
            if (hand != null && environment != null)
            {
                // Advance the hand timer
                hand_timer += Time.deltaTime;

                // Calculate the percentage of completion (0.0 to 1.0)
                float t = Mathf.Clamp01(hand_timer / hand_offset_time);

                // Calculate the distance we should have moved by now
                float current_offset = Mathf.Lerp(0f, final_hand_offset, t);

                // THE FIX: Slide the hand along the Environment's "Right" vector
                hand.transform.position = initial_hand_pos + (environment.right * current_offset);
            }
            else if (hand != null && environment == null)
            {
                Debug.LogWarning("Environment is missing in the Whackamole Manager! Please assign it.");
            }
        }
    }

    public void button_pressed(int button_index)
    {
        Debug.Log("Button is pressed!");

        // If we are in the middle of a countdown, ignore all button presses
        if (is_counting_down) return;

        // Check if the start button (index 0) was pressed
        if (button_index == 0 && !is_timer_running)
        {
            StartCoroutine(CountdownSequence());
            return;
        }

        // If the game is running and a valid mole is clicked
        if (is_timer_running)
        {
            if (!button_list[button_index].activeSelf) return;

            press_count++;

            // Disable the mole that was just pressed
            button_list[button_index].SetActive(false);

            // Check if we've reached the 'nth' press
            if (press_count >= n)
            {
                EndGame();
            }
            else
            {
                EnableRandomButton();
            }
        }
    }

    private IEnumerator CountdownSequence()
    {
        is_counting_down = true;

        button_list[0].SetActive(false);

        int countdown = 3;
        while (countdown > 0)
        {
            if (timer_object != null) timer_object.text = countdown.ToString();
            yield return new WaitForSeconds(1f);
            countdown--;
        }

        if (timer_object != null) timer_object.text = "GO!";
        yield return new WaitForSeconds(0.5f);

        is_counting_down = false;
        StartGame();
    }

    private void StartGame()
    {
        press_count = 0;
        elapsed_time = 0f;
        hand_timer = 0f;
        is_timer_running = true;

        EnableRandomButton();
    }

    private void EndGame()
    {
        is_timer_running = false;

        if (elapsed_time < best_score)
        {
            best_score = elapsed_time;
        }

        StartCoroutine(PostGameCooldownSequence());
    }

    private IEnumerator PostGameCooldownSequence()
    {
        foreach (GameObject btn in button_list)
        {
            btn.SetActive(false);
        }

        if (timer_object != null)
        {
            timer_object.text = "Put your hands together!";
        }

        yield return new WaitForSeconds(10f);

        ResetGame();
    }

    private void ResetGame()
    {
        foreach (GameObject btn in button_list)
        {
            btn.SetActive(false);
        }

        if (button_list.Count > 0)
        {
            button_list[0].SetActive(true);
        }

        // Reset the hand back to exactly where it was at the start
        if (hand != null)
        {
            hand.transform.position = initial_hand_pos;
        }

        if (timer_object != null)
        {
            string displayText = "Press Start!";
            if (best_score < float.MaxValue)
            {
                displayText += $"\nBest: {best_score:F2}";
            }
            timer_object.text = displayText;
        }
    }

    private void EnableRandomButton()
    {
        if (button_list.Count <= 1) return;

        int random_index;

        do
        {
            random_index = UnityEngine.Random.Range(1, button_list.Count);
        }
        while (button_list[random_index].activeSelf);

        button_list[random_index].SetActive(true);
    }

    private void UpdateTimerDisplay()
    {
        if (timer_object != null)
        {
            string displayText = $"Time: {elapsed_time:F2}";

            if (best_score < float.MaxValue)
            {
                displayText += $"\nBest: {best_score:F2}";
            }

            timer_object.text = displayText;
        }
    }
}