using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public enum TutorialAction
{
    LookAround,   // required = pixels de movimento do mouse
    Zoom,         // required = segundos segurando o botao direito
    Move,         // required = segundos andando (WASD)
    Jump,         // required = quantidade de pulos
    Run,          // required = segundos correndo (Shift + WASD)
    Shoot,        // required = quantidade de tiros
    StayInZone,   // required = segundos dentro de uma zona de controle
    Timed         // required = segundos na tela (mensagem informativa)
}

[System.Serializable]
public class TutorialStep
{
    [TextArea(1, 3)] public string message;
    public TutorialAction action;
    [Tooltip("Pixels, segundos ou quantidade, dependendo da acao")]
    public float required = 1f;
    [Tooltip("Pausa antes de mostrar este passo")]
    public float delayBefore = 0.5f;

    public TutorialStep(string message, TutorialAction action, float required, float delayBefore = 0.5f)
    {
        this.message = message;
        this.action = action;
        this.required = required;
        this.delayBefore = delayBefore;
    }
}

public class TutorialController : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text label;
    [Tooltip("CanvasGroup que sera usado para o fade. Se vazio, e adicionado automaticamente ao label.")]
    public CanvasGroup group;
    public float fadeInSpeed = 3f;
    public float fadeOutSpeed = 2f;
    [Tooltip("O texto vai ficando mais transparente conforme o jogador progride")]
    public bool dimWithProgress = true;
    [Range(0f, 1f)] public float minAlphaWhileDoing = 0.5f;

    [Header("Passos")]
    public List<TutorialStep> steps = new List<TutorialStep>();

    private ControlZone[] zones;

    // Preenche os passos padrao ao adicionar o componente (ou via menu de contexto > Reset)
    void Reset()
    {
        steps = new List<TutorialStep>
        {
            new TutorialStep("Olhe ao redor movendo o mouse", TutorialAction.LookAround, 600f),
            new TutorialStep("Mova-se com WASD", TutorialAction.Move, 2f),
            new TutorialStep("Pule com espaço", TutorialAction.Jump, 1f),
            new TutorialStep("Corra com Shift", TutorialAction.Run, 1.5f),
            new TutorialStep("Clique com o botão direito do mouse para dar zoom", TutorialAction.Zoom, 1f),
            new TutorialStep("Atire nos inimigos com o botão esquerdo do mouse", TutorialAction.Shoot, 3f),
            new TutorialStep("Fique nas zonas de controle para dominá-las", TutorialAction.StayInZone, 3f),
            new TutorialStep("Não deixe os inimigos dominarem as zonas", TutorialAction.Timed, 5f),
        };
    }

    void Start()
    {
        if (label == null)
        {
            Debug.LogWarning("TutorialController: arraste um TMP_Text para o campo 'label'.");
            enabled = false;
            return;
        }

        if (group == null)
        {
            group = label.GetComponent<CanvasGroup>();
            if (group == null) group = label.gameObject.AddComponent<CanvasGroup>();
        }

        group.alpha = 0f;
        zones = FindObjectsByType<ControlZone>(FindObjectsSortMode.None);
        StartCoroutine(RunTutorial());
    }

    IEnumerator RunTutorial()
    {
        // So comeca quando o jogo comecar
        while (!GameManager.GameStarted) yield return null;

        foreach (var step in steps)
        {
            if (step.delayBefore > 0f) yield return new WaitForSecondsRealtime(step.delayBefore);

            label.text = step.message;

            // Fade in
            while (group.alpha < 1f)
            {
                group.alpha = Mathf.MoveTowards(group.alpha, 1f, fadeInSpeed * Time.unscaledDeltaTime);
                yield return null;
            }

            // Espera o jogador cumprir a tarefa
            float progress = 0f;
            while (progress < step.required)
            {
                progress += Measure(step.action);

                if (dimWithProgress)
                {
                    float p = Mathf.Clamp01(progress / Mathf.Max(0.0001f, step.required));
                    group.alpha = Mathf.Lerp(1f, minAlphaWhileDoing, p);
                }
                yield return null;
            }

            // Fade out
            while (group.alpha > 0f)
            {
                group.alpha = Mathf.MoveTowards(group.alpha, 0f, fadeOutSpeed * Time.unscaledDeltaTime);
                yield return null;
            }
        }

        gameObject.SetActive(false);
    }

    // Retorna quanto de progresso foi feito NESTE frame
    float Measure(TutorialAction action)
    {
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb == null || mouse == null) return 0f;

        switch (action)
        {
            case TutorialAction.LookAround:
                return mouse.delta.ReadValue().magnitude;

            case TutorialAction.Zoom:
                return mouse.rightButton.isPressed ? Time.deltaTime : 0f;

            case TutorialAction.Move:
                return IsMoving(kb) ? Time.deltaTime : 0f;

            case TutorialAction.Jump:
                return kb[Key.Space].wasPressedThisFrame ? 1f : 0f;

            case TutorialAction.Run:
                return (kb[Key.LeftShift].isPressed && IsMoving(kb)) ? Time.deltaTime : 0f;

            case TutorialAction.Shoot:
                return mouse.leftButton.wasPressedThisFrame ? 1f : 0f;

            case TutorialAction.StayInZone:
                return PlayerInAnyZone() ? Time.deltaTime : 0f;

            case TutorialAction.Timed:
                return Time.deltaTime;
        }
        return 0f;
    }

    bool IsMoving(Keyboard kb)
    {
        return kb[Key.W].isPressed || kb[Key.A].isPressed ||
               kb[Key.S].isPressed || kb[Key.D].isPressed;
    }

    bool PlayerInAnyZone()
    {
        foreach (var z in zones)
            if (z != null && z.PlayerInside) return true;
        return false;
    }
}