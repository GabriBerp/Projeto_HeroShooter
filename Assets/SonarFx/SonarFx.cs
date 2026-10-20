//
// Sonar FX
//
// Copyright (C) 2013, 2014 Keijiro Takahashi
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Camera))]
public class SonarFx : MonoBehaviour
{
    public enum SonarMode { Directional, Spherical }
    [SerializeField] SonarMode _mode = SonarMode.Directional;
    public SonarMode mode { get { return _mode; } set { _mode = value; } }

    [SerializeField] Vector3 _direction = Vector3.forward;
    public Vector3 direction { get { return _direction; } set { _direction = value; } }

    [SerializeField] Vector3 _origin = Vector3.zero;
    public Vector3 origin { get { return _origin; } set { _origin = value; } }

    [SerializeField] Color _baseColor = new Color(0.2f, 0.2f, 0.2f, 0);
    public Color baseColor { get { return _baseColor; } set { _baseColor = value; } }

    [SerializeField] Color _waveColor = new Color(1.0f, 0.2f, 0.2f, 0);
    public Color waveColor { get { return _waveColor; } set { _waveColor = value; } }

    [SerializeField] float _waveAmplitude = 2.0f;
    public float waveAmplitude { get { return _waveAmplitude; } set { _waveAmplitude = value; } }

    [SerializeField] float _waveExponent = 22.0f;
    public float waveExponent { get { return _waveExponent; } set { _waveExponent = value; } }

    [SerializeField] float _waveInterval = 20.0f;
    public float waveInterval { get { return _waveInterval; } set { _waveInterval = value; } }

    [SerializeField] float _waveSpeed = 10.0f;
    public float waveSpeed { get { return _waveSpeed; } set { _waveSpeed = value; } }

    [SerializeField] Color _addColor = Color.black;
    public Color addColor { get { return _addColor; } set { _addColor = value; } }

    // Mantido so para nao quebrar o inspector do Editor (nao e mais usado)
    [SerializeField] Shader shader;

    // Indices na lista de Renderers do URP Asset
    [SerializeField] int _defaultRendererIndex = 0;
    [SerializeField] int _sonarRendererIndex = 1;

    // Duracao da transicao (segundos). 0 = corte seco.
    [SerializeField] float _fadeInTime = 0.6f;
    [SerializeField] float _fadeOutTime = 0.8f;

    // 0 = cena normal, 1 = sonar total (valor linear, antes da suavizacao)
    public float blend { get; private set; }
    float _target;

    int baseColorID;
    int waveColorID;
    int waveParamsID;
    int waveVectorID;
    int addColorID;
    int blendID;

    void Awake()
    {
        baseColorID = Shader.PropertyToID("_SonarBaseColor");
        waveColorID = Shader.PropertyToID("_SonarWaveColor");
        waveParamsID = Shader.PropertyToID("_SonarWaveParams");
        waveVectorID = Shader.PropertyToID("_SonarWaveVector");
        addColorID = Shader.PropertyToID("_SonarAddColor");
        blendID = Shader.PropertyToID("_SonarBlend");
    }

    // Use estes dois metodos em vez de mexer em "enabled"
    public void Show()
    {
        _target = 1f;
        enabled = true;
    }

    public void Hide(bool instant = false)
    {
        _target = 0f;
        if (instant) { blend = 0f; enabled = false; }
    }

    void OnEnable()
    {
        _target = 1f; // "enabled = true" direto tambem faz fade in
        GetComponent<Camera>().GetUniversalAdditionalCameraData().SetRenderer(_sonarRendererIndex);
        Update();
    }

    void OnDisable()
    {
        GetComponent<Camera>().GetUniversalAdditionalCameraData().SetRenderer(_defaultRendererIndex);
        blend = 0f;
        Shader.SetGlobalFloat(blendID, 0f);
    }

    void Update()
    {
        // Anima o blend em direcao ao alvo
        float time = (_target > blend) ? _fadeInTime : _fadeOutTime;
        blend = (time > 0f)
            ? Mathf.MoveTowards(blend, _target, Time.deltaTime / time)
            : _target;

        Shader.SetGlobalFloat(blendID, Mathf.SmoothStep(0f, 1f, blend));

        // Fade out terminou: volta ao renderer normal
        if (_target <= 0f && blend <= 0f)
        {
            enabled = false;
            return;
        }

        Shader.SetGlobalColor(baseColorID, _baseColor);
        Shader.SetGlobalColor(waveColorID, _waveColor);
        Shader.SetGlobalColor(addColorID, _addColor);

        var param = new Vector4(_waveAmplitude, _waveExponent, _waveInterval, _waveSpeed);
        Shader.SetGlobalVector(waveParamsID, param);

        if (_mode == SonarMode.Directional)
        {
            Shader.DisableKeyword("SONAR_SPHERICAL");
            Shader.SetGlobalVector(waveVectorID, _direction.normalized);
        }
        else
        {
            Shader.EnableKeyword("SONAR_SPHERICAL");
            Shader.SetGlobalVector(waveVectorID, _origin);
        }
    }
}