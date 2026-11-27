using System.Collections.Generic;
using UnityEngine;

public enum InteractionType { Alimentar, DarAgua, LimpiarArena, Acariciar, Tienda, SalirACorrer, Dormir }

// Poner en cualquier objeto de la casa con el que el dueño pueda interactuar (plato, arenero, gato, puerta...).
public class Interactable : MonoBehaviour
{
    public InteractionType type;
    public string label = "Interactuar";
    public float range = 2f;

    public static readonly List<Interactable> All = new List<Interactable>();

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);
}
