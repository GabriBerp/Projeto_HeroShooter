#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public class AliveBridge
{
    private static HttpListener listener;
    private static Thread listenerThread;
    private static readonly object lockObj = new object();
    private static string pendingCommandJson = null;
    private static readonly List<string> recentErrorLogs = new List<string>();

    static AliveBridge()
    {
        Application.runInBackground = true;
        Application.logMessageReceivedThreaded += OnLogReceived;
        StartServer();
        EditorApplication.update += Update;
    }

    private static void OnLogReceived(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception)
        {
            lock (lockObj)
            {
                string msg = logString.Length > 200 ? logString.Substring(0, 200) : logString;
                recentErrorLogs.Add(msg);
                if (recentErrorLogs.Count > 8) recentErrorLogs.RemoveAt(0);
            }
        }
    }

    private static void StartServer()
    {
        try
        {
            if (listener != null && listener.IsListening) return;
            listener = new HttpListener();
            listener.Prefixes.Add("http://127.0.0.1:8080/");
            listener.Start();
            listenerThread = new Thread(ListenLoop);
            listenerThread.IsBackground = true;
            listenerThread.Start();
            Debug.Log("⚡ [Alive Bridge] Unity 6.5 Live Bridge iniciado em http://127.0.0.1:8080/");
        }
        catch (Exception e)
        {
            Debug.LogWarning("⚠️ [Alive Bridge]: " + e.Message);
        }
    }

    private static string GetGameObjectDetails(GameObject go, int depth)
    {
        if (go == null || depth > 2) return "";
        var comps = go.GetComponents<Component>().Where(c => c != null).Select(c => c.GetType().Name).Take(6);
        string posStr = string.Format("({0:F1},{1:F1},{2:F1})", go.transform.position.x, go.transform.position.y, go.transform.position.z);
        string details = go.name + " [pos:" + posStr + "] (" + string.Join(", ", comps) + ")";
        if (go.transform.childCount > 0)
        {
            var childrenDetails = new List<string>();
            for (int i = 0; i < Math.Min(go.transform.childCount, 5); i++)
            {
                var childGO = go.transform.GetChild(i).gameObject;
                childrenDetails.Add(GetGameObjectDetails(childGO, depth + 1));
            }
            details += " -> [" + string.Join(" | ", childrenDetails) + "]";
        }
        return details;
    }

    private static void ListenLoop()
    {
        while (listener != null && listener.IsListening)
        {
            try
            {
                var context = listener.GetContext();
                var request = context.Request;
                var response = context.Response;
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

                if (request.HttpMethod == "OPTIONS")
                {
                    response.StatusCode = 200;
                    response.Close();
                    continue;
                }

                if (request.Url.AbsolutePath == "/status")
                {
                    string statusJson = "{\"status\":\"online\",\"version\":\"6.5\",\"editor\":\"Unity 6.5 Its Alive!\"}";
                    byte[] buffer = Encoding.UTF8.GetBytes(statusJson);
                    response.ContentType = "application/json";
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                    response.Close();
                    continue;
                }

                if (request.Url.AbsolutePath == "/logs")
                {
                    string logsJoined = "";
                    lock (lockObj)
                    {
                        logsJoined = string.Join(" | ", recentErrorLogs.Select(l => l.Replace((char)34, (char)39).Replace((char)10, (char)32).Replace((char)13, (char)32)));
                    }
                    string logsJson = "{\"status\":\"ok\",\"errors\":\"" + logsJoined + "\"}";
                    byte[] buffer = Encoding.UTF8.GetBytes(logsJson);
                    response.ContentType = "application/json";
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                    response.Close();
                    continue;
                }

                if (request.Url.AbsolutePath == "/project")
                {
                    string assetsPath = Application.dataPath;
                    var files = Directory.GetFiles(assetsPath, "*.*", SearchOption.AllDirectories)
                        .Where(f => !f.EndsWith(".meta") && !f.EndsWith(".DS_Store"))
                        .Select(f => f.Replace(Directory.GetParent(assetsPath).FullName + "/", "").Replace(Directory.GetParent(assetsPath).FullName + "\\", ""))
                        .Take(60);
                    string projectStr = string.Join(" ; ", files);
                    string projJson = "{\"status\":\"ok\",\"files\":\"" + projectStr.Replace((char)34, (char)39).Replace((char)10, (char)32).Replace((char)13, (char)32) + "\"}";
                    byte[] buffer = Encoding.UTF8.GetBytes(projJson);
                    response.ContentType = "application/json";
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                    response.Close();
                    continue;
                }

                if (request.Url.AbsolutePath == "/scene")
                {
                    var rootObjs = SceneManager.GetActiveScene().GetRootGameObjects();
                    var listStr = new List<string>();
                    foreach (var go in rootObjs.Take(15))
                    {
                        listStr.Add(GetGameObjectDetails(go, 0));
                    }
                    string sceneStr = string.Join(" ; ", listStr);
                    string sceneJson = "{\"status\":\"ok\",\"scene\":\"" + sceneStr.Replace((char)34, (char)39).Replace((char)10, (char)32).Replace((char)13, (char)32) + "\"}";
                    byte[] buffer = Encoding.UTF8.GetBytes(sceneJson);
                    response.ContentType = "application/json";
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                    response.Close();
                    continue;
                }

                if (request.Url.AbsolutePath == "/execute" && request.HttpMethod == "POST")
                {
                    using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        string body = reader.ReadToEnd();
                        lock (lockObj)
                        {
                            pendingCommandJson = body;
                        }
                    }
                    string okJson = "{\"status\":\"received\"}";
                    byte[] buffer = Encoding.UTF8.GetBytes(okJson);
                    response.ContentType = "application/json";
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                    response.Close();
                    continue;
                }

                response.StatusCode = 404;
                response.Close();
            }
            catch (Exception) {}
        }
    }

    private static void Update()
    {
        string commandToRun = null;
        lock (lockObj)
        {
            if (pendingCommandJson != null)
            {
                commandToRun = pendingCommandJson;
                pendingCommandJson = null;
            }
        }

        if (!string.IsNullOrEmpty(commandToRun))
        {
            ExecuteOnMainThread(commandToRun);
        }

        if (!EditorApplication.isCompiling && pendingScriptAttachPayload != null)
        {
            TryAttachScriptsToTargets(pendingScriptAttachPayload);
        }
    }

    private static void ExecuteOnMainThread(string jsonPayload)
    {
        try
        {
            EditorUtility.DisplayProgressBar("⚡ Alive Bridge Sincronizando", "Aplicando alterações na cena e compilando scripts...", 0.5f);
            Debug.Log("⚡ [Alive Bridge Executando]: " + jsonPayload);
            string text = jsonPayload.ToLower();

            GameObject targetGO = null;
            if (text.Contains("capsule"))
            {
                targetGO = GameObject.Find("Alive_Capsule") ?? GameObject.Find("Player_Capsule") ?? GameObject.Find("Capsule");
                if (targetGO == null) {
                    targetGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    targetGO.name = "Alive_Capsule";
                    Undo.RegisterCreatedObjectUndo(targetGO, "Create Capsule via Alive Bridge");
                }
            }
            if (text.Contains("plane"))
            {
                var p = GameObject.Find("Alive_Plane") ?? GameObject.Find("Plane");
                if (p == null) {
                    p = GameObject.CreatePrimitive(PrimitiveType.Plane);
                    p.name = "Alive_Plane";
                    Undo.RegisterCreatedObjectUndo(p, "Create Plane via Alive Bridge");
                }
                if (targetGO == null) targetGO = p;
            }
            if (text.Contains("sphere"))
            {
                var s = GameObject.Find("Alive_Sphere") ?? GameObject.Find("Sphere");
                if (s == null) {
                    s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    s.name = "Alive_Sphere";
                    Undo.RegisterCreatedObjectUndo(s, "Create Sphere via Alive Bridge");
                }
                if (targetGO == null) targetGO = s;
            }
            if (text.Contains("cylinder"))
            {
                var cy = GameObject.Find("Alive_Cylinder") ?? GameObject.Find("Cylinder");
                if (cy == null) {
                    cy = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    cy.name = "Alive_Cylinder";
                    Undo.RegisterCreatedObjectUndo(cy, "Create Cylinder via Alive Bridge");
                }
                if (targetGO == null) targetGO = cy;
            }
            if (text.Contains("cube"))
            {
                var cb = GameObject.Find("Alive_Cube") ?? GameObject.Find("Cube");
                if (cb == null) {
                    cb = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cb.name = "Alive_Cube";
                    Undo.RegisterCreatedObjectUndo(cb, "Create Cube via Alive Bridge");
                }
                if (targetGO == null) targetGO = cb;
            }

            if (targetGO == null) targetGO = Selection.activeGameObject;

            // 2. FÍSICA & RIGIDBODY
            if (text.Contains("rigidbody") && targetGO != null)
            {
                if (targetGO.GetComponent<Rigidbody>() == null)
                {
                    Undo.AddComponent<Rigidbody>(targetGO);
                }
            }

            // 3. UI CANVAS
            if (text.Contains("create_canvas"))
            {
                var canvasGO = new GameObject("Alive_Canvas");
                var canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas via Alive Bridge");
            }

            // 4. ANIMATOR CONTROLLER (Assets/Project/Data/Animations/AC_Player.controller)
            if (text.Contains("animator"))
            {
                string animDir = "Assets/Project/Data/Animations";
                if (!Directory.Exists(animDir)) Directory.CreateDirectory(animDir);
                string path = animDir + "/AC_Character.controller";
                var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(path);
                controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
                controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
                controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
                var rootStateMachine = controller.layers[0].stateMachine;
                var idleState = rootStateMachine.AddState("Idle");
                rootStateMachine.AddState("Run");
                rootStateMachine.defaultState = idleState;
                Undo.RegisterCreatedObjectUndo(controller, "Create Animator Controller");
            }

            // 5. POST-PROCESSING VOLUME (URP/HDRP via Reflection seguro sem CS0246)
            if (text.Contains("postprocess"))
            {
                var volGO = new GameObject("Alive_PostProcessVolume");
                var volumeType = Type.GetType("UnityEngine.Rendering.Volume, Unity.RenderPipelines.Core.Runtime") 
                              ?? Type.GetType("UnityEngine.Rendering.Volume, UnityEngine.CoreModule");
                if (volumeType != null)
                {
                    volGO.AddComponent(volumeType);
                }
                Undo.RegisterCreatedObjectUndo(volGO, "Create PostProcess Volume");
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            pendingScriptAttachPayload = jsonPayload;
            TryAttachScriptsToTargets(jsonPayload);
            Debug.Log("🟢 [Alive Bridge] Alterações concluídas com sucesso no Unity 6.5!");
        }
        catch (Exception ex)
        {
            Debug.LogError("🔥 [Alive Bridge Erro]: " + ex.Message);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private static string pendingScriptAttachPayload = null;

    private static void TryAttachScriptsToTargets(string jsonPayload)
    {
        if (string.IsNullOrEmpty(jsonPayload)) return;
        try
        {
            string text = jsonPayload.ToLower();
            GameObject targetGO = null;
            if (text.Contains("capsule")) targetGO = GameObject.Find("Alive_Capsule") ?? GameObject.Find("Player_Capsule") ?? GameObject.Find("Player") ?? GameObject.Find("Capsule");
            else if (text.Contains("cube")) targetGO = GameObject.Find("Alive_Cube") ?? GameObject.Find("Cube");
            else if (text.Contains("sphere")) targetGO = GameObject.Find("Alive_Sphere") ?? GameObject.Find("Sphere");
            else if (text.Contains("plane")) targetGO = GameObject.Find("Alive_Plane") ?? GameObject.Find("Plane");
            if (targetGO == null) targetGO = Selection.activeGameObject;

            if (targetGO != null)
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name == "Assembly-CSharp");
                foreach (var asm in assemblies)
                {
                    var types = asm.GetTypes().Where(t => typeof(MonoBehaviour).IsAssignableFrom(t) && !t.IsAbstract);
                    foreach (var t in types)
                    {
                        if (t.Name != "AliveBridge" && text.Contains(t.Name.ToLower()))
                        {
                            if (targetGO.GetComponent(t) == null)
                            {
                                Undo.AddComponent(targetGO, t);
                                Debug.Log("⚡ [Alive Bridge] Script " + t.Name + " anexado automaticamente no GameObject " + targetGO.name + "!");
                                pendingScriptAttachPayload = null;
                            }
                        }
                    }
                }
            }
        }
        catch(Exception) {}
    }
}
#endif