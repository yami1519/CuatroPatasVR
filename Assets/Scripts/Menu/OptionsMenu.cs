using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Panel de Opciones del menú: volumen general y borrar progreso.
public class OptionsMenu : MonoBehaviour
{
    public Slider volumeSlider;
    public TMP_Text volumeLabel;
    public TMP_Text infoText;

    public const string VolumeKey = "CuatroPatas_Volumen";

    void OnEnable()
    {
        float v = PlayerPrefs.GetFloat(VolumeKey, 0.8f);
        AudioListener.volume = v;
        if (volumeSlider)
        {
            volumeSlider.SetValueWithoutNotify(v);
            volumeSlider.onValueChanged.RemoveListener(SetVolume);
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }
        UpdateLabel(v);
        if (infoText) infoText.text = "";
    }

    public void SetVolume(float v)
    {
        AudioListener.volume = v;
        PlayerPrefs.SetFloat(VolumeKey, v);
        PlayerPrefs.Save();
        UpdateLabel(v);
    }

    public void ResetProgress()
    {
        GameManager.Get().ResetGame();
        if (infoText) infoText.text = "Progreso borrado. ¡Nueva partida lista!";
    }

    void UpdateLabel(float v)
    {
        if (volumeLabel) volumeLabel.text = $"Volumen: {Mathf.RoundToInt(v * 100)}%";
    }
}
