using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class NetworkManager : MonoBehaviour
{
    [Header("API Config")]
    private string baseUrl = "https://sid-restapi.onrender.com/api";

    [Header("UI Panels")]
    public GameObject panelAuth;
    public GameObject panelUser;

    [Header("Input Fields - Auth")]
    public TMP_InputField inputNombre;
    public TMP_InputField inputCorreo;
    public TMP_InputField inputPassword;

    [Header("Input Fields - Game/User")]
    public TMP_InputField inputScore;
    public TMP_Text textWelcome;
    public TMP_Text textLeaderboard;

    private string authToken = "";

    void Start()
    {
        // Verificar si ya existe un token guardado en sesión previa
        if (PlayerPrefs.HasKey("auth_token"))
        {
            authToken = PlayerPrefs.GetString("auth_token");
            panelAuth.SetActive(false);
            panelUser.SetActive(true);
            StartCoroutine(GetLeaderboard());
        }
        else
        {
            panelAuth.SetActive(true);
            panelUser.SetActive(false);
        }
    }

    #region BOTONES
    public void OnClickRegister()
    {
        StartCoroutine(RegisterCoroutine());
    }

    public void OnClickLogin()
    {
        StartCoroutine(LoginCoroutine());
    }

    public void OnClickLogout()
    {
        PlayerPrefs.DeleteKey("auth_token");
        authToken = "";
        panelAuth.SetActive(true);
        panelUser.SetActive(false);
    }

    public void OnClickUpdateScore()
    {
        StartCoroutine(UpdateScoreCoroutine());
    }
    #endregion

    #region CORRUTINAS HTTP

    // 1. REGISTRO
    IEnumerator RegisterCoroutine()
    {
        AuthData data = new AuthData();
        data.username = inputNombre.text;
        data.email = inputCorreo.text;
        data.password = inputPassword.text;

        string json = JsonUtility.ToJson(data);

        using (UnityWebRequest request = UnityWebRequest.PostWwwForm(baseUrl + "/usuarios", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Registro exitoso. Procede a Iniciar Sesión.");
            }
            else
            {
                Debug.LogError("Error en Registro: " + request.error);
            }
        }
    }

    // 2. LOGIN
    IEnumerator LoginCoroutine()
    {
        AuthData data = new AuthData();
        data.email = inputCorreo.text;
        data.password = inputPassword.text;

        string json = JsonUtility.ToJson(data);

        using (UnityWebRequest request = UnityWebRequest.PostWwwForm(baseUrl + "/auth/login", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                AuthResponse res = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text);
                authToken = res.token;
                
                // Guardar token en almacenamiento local
                PlayerPrefs.SetString("auth_token", authToken);

                panelAuth.SetActive(false);
                panelUser.SetActive(true);

                StartCoroutine(GetLeaderboard());
            }
            else
            {
                Debug.LogError("Error en Login: " + request.error);
            }
        }
    }

    // 3. ACTUALIZAR SCORE
    IEnumerator UpdateScoreCoroutine()
    {
        ScoreData data = new ScoreData();
        data.score = int.Parse(inputScore.text);

        string json = JsonUtility.ToJson(data);

        using (UnityWebRequest request = UnityWebRequest.PostWwwForm(baseUrl + "/usuarios", "PATCH"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            // Enviar Token de Autenticación en la Cabecera
            request.SetRequestHeader("x-token", authToken);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Score actualizado exitosamente.");
                StartCoroutine(GetLeaderboard());
            }
            else
            {
                Debug.LogError("Error al actualizar score: " + request.error);
            }
        }
    }

    // 4. OBTENER TABLA DE POSICIONES Y ORDENAR
    IEnumerator GetLeaderboard()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(baseUrl + "/usuarios"))
        {
            request.SetRequestHeader("x-token", authToken);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResult = request.downloadHandler.text;
                
                // Formatear JSON si el API devuelve directamente una lista en vez de objeto
                if (jsonResult.StartsWith("["))
                {
                    jsonResult = "{\"usuarios\":" + jsonResult + "}";
                }

                UserListResponse list = JsonUtility.FromJson<UserListResponse>(jsonResult);

                // Ordenar usuarios de Mayor a Menor por puntaje usando LINQ
                List<UserData> sortedList = list.usuarios.OrderByDescending(u => u.score).ToList();

                textLeaderboard.text = "--- TABLA DE PUNTAJES ---\n";
                foreach (UserData user in sortedList)
                {
                    textLeaderboard.text += $"{user.username}: {user.score}\n";
                }
            }
            else
            {
                Debug.LogError("Error al consultar la tabla: " + request.error);
            }
        }
    }

    #endregion
}