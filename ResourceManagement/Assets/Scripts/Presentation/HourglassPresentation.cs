using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Presentation
{
    public class HourglassPresentation : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI timeRemaining;
        [SerializeField] Image hourglass;
        [SerializeField] Sprite[] frames;
        [SerializeField] Image sand;

        private float maxTimeSeconds = 180;

        internal void Initialize(float maxTimeSeconds)
        {
            this.maxTimeSeconds = maxTimeSeconds;
            hourglass.sprite = frames[0];
            sand.enabled = true;
        }

        private void SetTime(float timeRemaining)
        {
            int minutes = Mathf.FloorToInt(timeRemaining / 60);
            int seconds = Mathf.FloorToInt(timeRemaining % 60);

            this.timeRemaining.SetText($"{minutes:00}:{seconds:00}");
        }

        // Always get latest from server I suppose!
        internal void UpdateTime(float timeRemaining)
        {
            SetTime(timeRemaining);

            if(timeRemaining <= float.Epsilon )
            {
                hourglass.sprite = frames[4];
                sand.enabled = false;
            }
            else if (timeRemaining <= (maxTimeSeconds * 0.25f))
            {
                hourglass.sprite = frames[3];
            }
            else if (timeRemaining <= (maxTimeSeconds * 0.5f))
            {
                hourglass.sprite = frames[2];
            }
            else if (timeRemaining <= (maxTimeSeconds * 0.75f))
            {
                hourglass.sprite = frames[1];
            }
        }
    }
}