using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PatiDostum
{
    /// <summary>
    /// Okulda komut dersi: sıra hafızası. Renkli düğmeler sırayla yanar; çocuk aynı sırayla basar.
    /// Doğruysa ders başarılı (Training.Practice), yanlışsa az da olsa ilerler.
    /// </summary>
    public class SimonPanel : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private PetController owner;
        [SerializeField] private Image[] pads;
        [SerializeField] private Text prompt;
        [Tooltip("Sıra çocuğa gelince basılacak pedi gösteren el")]
        [SerializeField] private GameCoach coach;

        private readonly List<int> sequence = new List<int>();
        private string command;
        private int step;
        private bool listening;
        private Color[] baseColors;

        private void Awake()
        {
            root.SetActive(false);
            baseColors = new Color[pads.Length];
            for (int i = 0; i < pads.Length; i++) baseColors[i] = pads[i].color;
            // Bir kez: yanlış dersler arka arkaya sayılsın (her açılışta sıfırlanmasın)
            if (coach != null) coach.Begin("simon");
        }

        private void Update()
        {
            if (listening && coach != null && coach.Wanted) coach.Point(pads[sequence[step]].transform.position);
        }

        public void Open(string commandId)
        {
            command = commandId;
            root.SetActive(true);
            StartCoroutine(Show());
        }

        public void Close()
        {
            StopAllCoroutines();
            ResetPads();
            listening = false;
            root.SetActive(false);
        }

        public void Press(int pad)
        {
            if (!listening) return;
            StartCoroutine(Flash(pad, 0.18f));
            if (sequence[step] != pad)
            {
                if (coach != null) coach.Fail();
                Finish(false);
                return;
            }
            if (coach != null) coach.Success();
            step++;
            if (step >= sequence.Count) Finish(true);
        }

        private IEnumerator Show()
        {
            listening = false;
            sequence.Clear();
            for (int i = 0; i < MiniGameRules.SimonLength; i++) sequence.Add(Random.Range(0, pads.Length));
            prompt.text = Loc.Get("simon_watch");
            ResetPads();
            yield return new WaitForSecondsRealtime(0.7f);
            foreach (int pad in sequence)
            {
                yield return Flash(pad, 0.45f);
                yield return new WaitForSecondsRealtime(0.2f);
            }
            prompt.text = Loc.Get("simon_go");
            step = 0;
            listening = true;
        }

        private IEnumerator Flash(int pad, float seconds)
        {
            pads[pad].color = Color.Lerp(baseColors[pad], Color.white, 0.6f);
            pads[pad].rectTransform.localScale = Vector3.one * 1.08f;
            Sound.Play("pad_" + pad, 0.8f, 1f);   // her pedin kendi notası: sesten de hatırlanır
            yield return new WaitForSecondsRealtime(seconds);
            pads[pad].color = baseColors[pad];
            pads[pad].rectTransform.localScale = Vector3.one;
        }

        private void ResetPads()
        {
            for (int i = 0; i < pads.Length; i++)
            {
                pads[i].color = baseColors[i];
                pads[i].rectTransform.localScale = Vector3.one;
            }
        }

        private void Finish(bool success)
        {
            listening = false;
            Close();
            owner.FinishLesson(command, success);
        }
    }
}
