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

    [Tooltip("The object that will slide along the X axis during gameplay.")]
    public GameObject hand;

    [Header("Game Settings")]
    public int n = 10; // Number of times a mole is pressed

    [Tooltip("The final X position the hand will reach.")]
    public float final_hand_offset = 500f;
    [Tooltip("How long (in seconds) it takes the hand to reach the final offset.")]
    public float hand_offset_time = 5f;

    [Header("Game Variables")]
    private float elapsed_time = 0f;
    private float best_score = float.MaxValue;
    private int press_count = 0;
    private bool is_timer_running = false;
    private bool is_counting_down = false;

    // Tracks the time specifically for the hand's movement
    private float hand_timer = 0f;

    void Start()
    {
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
            if (hand != null)
            {
                // Advance the hand timer
                hand_timer += Time.deltaTime;

                // Calculate the percentage of completion (0.0 to 1.0)
                float t = Mathf.Clamp01(hand_timer / hand_offset_time);

                // Lerp the X position
                float current_x = Mathf.Lerp(0f, final_hand_offset, t);

                // Apply the new position while keeping the original Y and Z
                hand.transform.localPosition = new Vector3(current_x, hand.transform.localPosition.y, hand.transform.localPosition.z);
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
            // THE FIX: If the button is already inactive, ignore the click!
            // This prevents double-firing from spawning extra moles.
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

        // Disable the start button immediately so it can't be spammed
        button_list[0].SetActive(false);

        // 3-2-1 Countdown Loop
        int countdown = 3;
        while (countdown > 0)
        {
            if (timer_object != null) timer_object.text = countdown.ToString();
            yield return new WaitForSeconds(1f);
            countdown--;
        }

        // Display GO! briefly
        if (timer_object != null) timer_object.text = "GO!";
        yield return new WaitForSeconds(0.5f);

        is_counting_down = false;
        StartGame();
    }

    private void StartGame()
    {
        press_count = 0;
        elapsed_time = 0f;
        hand_timer = 0f; // Reset the hand timer for the new round
        is_timer_running = true;

        // Bring up the first mole
        EnableRandomButton();
    }

    private void EndGame()
    {
        is_timer_running = false;

        // Update the best score if the player was faster this round
        if (elapsed_time < best_score)
        {
            best_score = elapsed_time;
        }

        // Start the cooldown phase instead of an immediate reset
        StartCoroutine(PostGameCooldownSequence());
    }

    // NEW COROUTINE: Handles the 10-second wait at the end of the game
    private IEnumerator PostGameCooldownSequence()
    {
        // Deactivate all buttons during the cooldown
        foreach (GameObject btn in button_list)
        {
            btn.SetActive(false);
        }

        // Display the specific post-game text
        if (timer_object != null)
        {
            timer_object.text = "Put your hands together!";
        }

        // Wait for exactly 10 seconds
        yield return new WaitForSeconds(10f);

        // Finally, reset the game board for the next player
        ResetGame();
    }

    private void ResetGame()
    {
        // Deactivate all buttons
        foreach (GameObject btn in button_list)
        {
            btn.SetActive(false);
        }

        // Reactivate ONLY the start button (which is at index 0)
        if (button_list.Count > 0)
        {
            button_list[0].SetActive(true);
        }

        // Reset the hand's X position back to 0
        if (hand != null)
        {
            hand.transform.localPosition = new Vector3(0f, hand.transform.localPosition.y, hand.transform.localPosition.z);
        }

        // Reset the display to show the starting text
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
        if (button_list.Count <= 1) return; // Failsafe

        int random_index;

        // Loop to find a random button (from index 1 to the end) that isn't already active
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