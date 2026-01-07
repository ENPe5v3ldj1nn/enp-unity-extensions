using TMPro;
using UnityEngine;

public class TextGradient : MonoBehaviour
{
    [SerializeField] private string _targetWord = ""; // Слово, до якого застосовувати градієнт

    private void Start()
    {
        ApplyGradient();
    }

    void ApplyGradient()
    {
        TMP_Text textComponent = GetComponent<TMP_Text>();
        textComponent.ForceMeshUpdate();
        TMP_TextInfo textInfo = textComponent.textInfo;

        // Знайти індекси початку і кінця слова
        int startCharIndex = -1;
        int endCharIndex = -1;
        string text = textComponent.text;

        int wordStartIndex = text.IndexOf(_targetWord);
        if (wordStartIndex != -1)
        {
            startCharIndex = wordStartIndex;
            endCharIndex = wordStartIndex + _targetWord.Length - 1;
        }

        if (startCharIndex == -1 || endCharIndex == -1)
        {
            Debug.LogWarning($"Слово '{_targetWord}' не знайдено в тексті.");
            return;
        }

        // Отримати градієнтні кольори
        Color[] steps = GetGradients(textComponent.colorGradient.topLeft, textComponent.colorGradient.topRight, endCharIndex - startCharIndex + 1);
        VertexGradient[] gradients = new VertexGradient[steps.Length];
        for (int i = 0; i < steps.Length - 1; i++)
        {
            gradients[i] = new VertexGradient(steps[i], steps[i + 1], steps[i], steps[i + 1]);
        }

        Color32[] colors;
        for (int index = 0; index < textInfo.characterCount; index++)
        {
            int materialIndex = textInfo.characterInfo[index].materialReferenceIndex;
            colors = textInfo.meshInfo[materialIndex].colors32;
            int vertexIndex = textInfo.characterInfo[index].vertexIndex;

            // Перевіряємо, чи індекс символу входить у визначений діапазон
            if (textInfo.characterInfo[index].isVisible && index >= startCharIndex && index <= endCharIndex)
            {
                colors[vertexIndex + 0] = gradients[index - startCharIndex].bottomLeft;
                colors[vertexIndex + 1] = gradients[index - startCharIndex].topLeft;
                colors[vertexIndex + 2] = gradients[index - startCharIndex].bottomRight;
                colors[vertexIndex + 3] = gradients[index - startCharIndex].topRight;
                textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
            }
        }
    }

    public static Color[] GetGradients(Color start, Color end, int steps)
    {
        Color[] result = new Color[steps];
        float r = ((end.r - start.r) / (steps - 1));
        float g = ((end.g - start.g) / (steps - 1));
        float b = ((end.b - start.b) / (steps - 1));
        float a = ((end.a - start.a) / (steps - 1));
        for (int i = 0; i < steps; i++)
        {
            result[i] = new Color(start.r + (r * i), start.g + (g * i), start.b + (b * i), start.a + (a * i));
        }
        return result;
    }
}
