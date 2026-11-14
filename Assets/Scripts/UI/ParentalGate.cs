using System;
using UnityEngine;
using UnityEngine.UI;

namespace PatiDostum
{
    /// <summary>
    /// Ebeveyn kapısı: çocuğun tek başına geçemeyeceği bir çarpma sorusu ("17 × 8 = ?", 4 seçenek; oyuncular çarpım
    /// tablosunu bilen yaşta olabilir, sorular iki basamaklı). İki yanlışta kapanır: rastgele basarak geçilmesin.
    /// Ebeveyn ayarlarından önce çıkar; M4'te gerçek paralı her ekrandan önce de çıkacak (plan §7).
    /// </summary>
    public class ParentalGate : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text question;
        [SerializeField] private Text[] answers;
        [SerializeField] private Text message;

        private Action onPass;
        private const int MaxWrong = 2;
        private int correct, wrong;
        private readonly int[] values = new int[4];

        private void Awake()
        {
            root.SetActive(false);
        }

        public void Ask(Action passed)
        {
            onPass = passed;
            wrong = 0;
            message.text = "";
            NewQuestion();
            root.SetActive(true);
        }

        public void Cancel()
        {
            onPass = null;
            root.SetActive(false);
        }

        public void Answer(int index)
        {
            if (values[index] != correct)
            {
                wrong++;
                if (wrong >= MaxWrong)
                {
                    Sound.Play("error", 0.7f);
                    Cancel();
                    return;
                }
                message.text = Loc.Get("gate_wrong");
                NewQuestion();
                return;
            }
            root.SetActive(false);
            Action passed = onPass;
            onPass = null;
            passed?.Invoke();
        }

        private void NewQuestion()
        {
            int a = UnityEngine.Random.Range(12, 20), b = UnityEngine.Random.Range(6, 10);
            correct = a * b;
            question.text = Loc.Format("gate_question", a, b);

            // Yanlış seçenekler doğruya yakın ama farklı
            int[] offsets = { a, -b, 10, -1, 1, -10, b };
            values[0] = correct;
            int filled = 1;
            for (int i = 0; filled < values.Length && i < offsets.Length; i++)
            {
                int candidate = correct + offsets[i];
                if (candidate > 0 && Array.IndexOf(values, candidate, 0, filled) < 0) values[filled++] = candidate;
            }
            for (int i = values.Length - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (values[i], values[j]) = (values[j], values[i]);
            }
            for (int i = 0; i < answers.Length; i++) answers[i].text = values[i].ToString();
        }
    }
}
