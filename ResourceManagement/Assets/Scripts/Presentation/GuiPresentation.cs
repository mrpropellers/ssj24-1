using System;
using System.Collections;
using System.Collections.Generic;
using NetCode;
using Simulation;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace Presentation
{
    public class GuiPresentation : MonoBehaviour
    {
        [SerializeField] GameObject container;

        [SerializeField]
        private TextMeshProUGUI score;

        [SerializeField]
        private Image scoreFill;

        [SerializeField]
        private TextMeshProUGUI rats;

        [SerializeField]
        private GameObject scoreboard;

        [SerializeField]
        private bool testGui;

        [SerializeField] private HourglassPresentation hourglass;

        private const float MAX_TIME = 180f;
        private const float MAX_SCORE = 21.0f;
        private int currentScore = 0;
        private int currentRats = 0;

        private bool isStarted = false;


        private void Awake()
        {
            if (testGui)
                StartCoroutine(TestGui());

            container.SetActive(false);
        }

        private void Start()
        {
            SetRats(currentRats);
            SetScore(currentScore);

            // TODO: Configured?  Via server?
            hourglass.Initialize(MAX_TIME);
        }

        private void Update()
        {
            if (isStarted)
                return;

            if( GameplaySceneLoader.GameCanStart )
            {
                container.SetActive(true);
                isStarted = true;
            }

            //if( GameplaySceneLoader.GameStarted )
            //{
            //    container.SetActive(false);
            //}
        }

        public void SetTime(float timeRemaining)
        {
            hourglass.UpdateTime(timeRemaining);
        }

        public void SetScore(int score)
        {
            this.score.SetText($"{score}");

            float normal = ((float)score) / MAX_SCORE;
            scoreFill.fillAmount = normal;
        }

        public void SetRats(int rats)
        {
            this.rats.SetText($"{rats}");
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
            SetRats(0);
            for (int i = 0; i < 5; i++)
            {
                AddRat();
                yield return new WaitForSeconds(1);
            }
            for (int i = 0; i < 5; i++)
            {
                RemoveRat();
                yield return new WaitForSeconds(1);
            }


            SetScore(0);
            for (int i = 0; i <= 10; i++)
            {
                SetTime(10 - i);
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

        internal void RemoveRat()
        {
            currentRats--;
            SetRats(currentRats);
        }

        internal void AddRat()
        {
            currentRats++;
            SetRats(currentRats);
        }

        internal void UpdateScore()
        {
            currentScore++;
            SetScore(currentScore);
        }
    }
}