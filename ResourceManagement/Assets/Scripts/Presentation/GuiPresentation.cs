using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

public class GuiPresentation : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI timeRemaining;

    [SerializeField]
    private TextMeshProUGUI score;

    [SerializeField]
    private GameObject scoreboard;

    [SerializeField]
    private bool testGui;

    private void Awake()
    {
        if (testGui)
            StartCoroutine(TestGui());
    }

    public void SetTime(float timeRemaining)
    {
        int minutes = Mathf.FloorToInt(timeRemaining / 60);
        int seconds = Mathf.FloorToInt(timeRemaining % 60);

        this.timeRemaining.SetText($"Time: {minutes:00}:{seconds:00}");
    }

    public void SetScore(int score)
    {
        this.score.SetText($"Rat Score: {score}");
    }

    public void EnableScoreboard(List<string> playerNames, List<int> playerScores)
    {
        Assert.AreEqual(playerNames.Count, playerScores.Count);
        scoreboard.SetActive(true);
        for (int i = 0; i < 5; i++)
        {
            TextMeshProUGUI scoreText = scoreboard.transform.Find($"{i + 1}Score").GetComponent<TextMeshProUGUI>();
            scoreText.SetText("");
            if (i >= playerNames.Count || i >= playerScores.Count)
            {
                continue;
            }
            scoreText.SetText($"{i + 1}: {playerNames[i]} - {playerScores[i]} rats");
        }
    }

    public void DisableScoreboard()
    {
        scoreboard.SetActive(false);
    }

    private IEnumerator TestGui()
    {
        SetScore(0);
        for (int i = 0; i < 5; i++)
        {
            SetTime(5 - i);
            SetScore(i);
            yield return new WaitForSeconds(1);
        }

        yield return new WaitForSeconds(1);

        List<string> testNames = new() { "Testerbro", "JohnnyLongNameMcgeeWahoo", "Hi", "Cool", "Pal", "CantSeeMe" };
        List<int> testScores = new() { 420, 69, 3, 2, 1, 0 };
        EnableScoreboard(testNames, testScores);
        yield return new WaitForSeconds(5);
        DisableScoreboard();
    }
}
