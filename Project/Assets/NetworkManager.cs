using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class NetworkManager : MonoBehaviour
{
    // Singleton para acceso global desde el script del Dinosaurio
    public static NetworkManager Instance;

    [Header("API Config")]
    private string baseUrl = "http://localhost:8080/api";

    [Header("UI Panels & Containers")]
    public GameObject panelAuth;
    public GameObject panelUser;
    public GameObject gameContainer; // Referencia al objeto GameManager de la escena

    [Header("Input Fields - Auth")]
    public TMP_InputField inputNombre;
    public TMP_InputField inputPassword;

    [Header("Input Fields - Game/User")]
    public TMP_InputField inputScore;
    public TMP_Text textWelcome;
    public TMP_Text textLeaderboard;

    private string authToken = "";
    private string currentUsername = "";

    void Awake()
    {
        // Inicialización del Singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Verificar si existe sesión guardada
        if (PlayerPrefs.HasKey("auth_token"))
        {
            authToken = PlayerPrefs.GetString("auth_token");
            currentUsername = PlayerPrefs.GetString("auth_username", "");

            if (textWelcome != null) textWelcome.text = "Bienvenid@ " + currentUsername;

            panelAuth.SetActive(false);
            panelUser.SetActive(true);
            if (gameContainer != null) gameContainer.SetActive(false);

            StartCoroutine(GetLeaderboard());
        }
        else
        {
            panelAuth.SetActive(true);
            panelUser.SetActive(false);
            if (gameContainer != null) gameContainer.SetActive(false);
        }
    }

    #region MÉTODOS PÚBLICOS E INTERFAZ

    // Llamado directamente por DinoController.cs cuando el jugador pierde
    public void SubmitGameScore(int finalScore)
    {
        if (panelUser != null) panelUser.SetActive(true);
        if (gameContainer != null) gameContainer.SetActive(false);

        // Pasamos la puntuación directamente a la corrutina
        StartCoroutine(UpdateScoreCoroutine(finalScore));
    }

    // Método opcional para asignar a un botón "Jugar" en la UI
    public void OnClickStartGame()
    {
        if (panelUser != null) panelUser.SetActive(false);
        if (gameContainer != null) gameContainer.SetActive(true);
    }

    #endregion

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
        PlayerPrefs.DeleteKey("auth_username");
        authToken = "";
        currentUsername = "";

        panelAuth.SetActive(true);
        panelUser.SetActive(false);
        if (gameContainer != null) gameContainer.SetActive(false);
    }


    #endregion

    #region CORRUTINAS HTTP

    // 1. REGISTRO.
    IEnumerator RegisterCoroutine()
    {
        AuthData authData = new AuthData();
        authData.username = inputNombre.text.Trim();
        authData.password = inputPassword.text.Trim();

        string jsonData = JsonUtility.ToJson(authData);

        using (UnityWebRequest www = UnityWebRequest.Post(baseUrl + "/usuarios", jsonData, "application/json"))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error en Registro: " + www.error + " | " + www.downloadHandler.text);
            }
            else
            {
                Debug.Log("¡Registro exitoso! Iniciando sesión automáticamente...");
                StartCoroutine(LoginCoroutine());
            }
        }
    }

    // 2. LOGIN
    IEnumerator LoginCoroutine()
    {
        AuthData authData = new AuthData();
        authData.username = inputNombre.text.Trim();
        authData.password = inputPassword.text.Trim();

        string jsonData = JsonUtility.ToJson(authData);

        using (UnityWebRequest www = UnityWebRequest.Post(baseUrl + "/auth/login", jsonData, "application/json"))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error en Login: " + www.error + " | " + www.downloadHandler.text);
            }
            else
            {
                Debug.Log("¡Login exitoso!: " + www.downloadHandler.text);
                UserResponse userResponse = JsonUtility.FromJson<UserResponse>(www.downloadHandler.text);

                authToken = userResponse.token;
                currentUsername = userResponse.usuario.username;

                PlayerPrefs.SetString("auth_token", authToken);
                PlayerPrefs.SetString("auth_username", currentUsername);

                if (textWelcome != null) textWelcome.text = "Bienvenid@ " + currentUsername;

                panelAuth.SetActive(false);
                panelUser.SetActive(true);
                if (gameContainer != null) gameContainer.SetActive(false);

                StartCoroutine(GetLeaderboard());
            }
        }
    }

    // 3. ACTUALIZAR SCORE (PATCH)
    // Sobrecarga para recibir el entero directamente desde el juego
IEnumerator UpdateScoreCoroutine(int scoreValue)
{
    ScoreData innerData = new ScoreData();
    innerData.score = scoreValue;

    PatchUserRequest requestBody = new PatchUserRequest();
    requestBody.username = currentUsername;
    requestBody.data = innerData;

    string json = JsonUtility.ToJson(requestBody);

    using (UnityWebRequest www = new UnityWebRequest(baseUrl + "/usuarios", "PATCH"))
    {
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();

        www.SetRequestHeader("Content-Type", "application/json");
        www.SetRequestHeader("x-token", authToken);

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("¡Score actualizado con éxito!");
            StartCoroutine(GetLeaderboard());
        }
        else
        {
            Debug.LogError("Error al actualizar score: " + www.error + " | " + www.downloadHandler.text);
        }
    }
}

// Mantenemos la versión sin parámetros por si la llamas desde un botón manual
public void OnClickUpdateScore()
{
    if (inputScore != null && !string.IsNullOrEmpty(inputScore.text))
    {
        StartCoroutine(UpdateScoreCoroutine(int.Parse(inputScore.text.Trim())));
    }
}

    // 4. TABLA DE POSICIONES
    IEnumerator GetLeaderboard()
    {
        using (UnityWebRequest www = UnityWebRequest.Get(baseUrl + "/usuarios"))
        {
            www.SetRequestHeader("x-token", authToken);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string jsonResult = www.downloadHandler.text;
                Debug.Log("RESPUESTA GET LEADERBOARD: " + jsonResult);

                if (jsonResult.StartsWith("["))
                {
                    jsonResult = "{\"usuarios\":" + jsonResult + "}";
                }

                UserListResponse list = JsonUtility.FromJson<UserListResponse>(jsonResult);

                List<UserData> sortedList = list.usuarios.OrderByDescending(u => u.GetRealScore()).ToList();

                textLeaderboard.text = "";
                foreach (UserData user in sortedList)
                {
                    textLeaderboard.text += $"{user.username}: {user.GetRealScore()}\n";
                }
            }
            else
            {
                Debug.LogError("Error al obtener tabla: " + www.error);
            }
        }
    }

    #endregion
}

// ==========================================
// ESTRUCTURAS DE DATOS DE LA API
// ==========================================
[Serializable]
public class AuthData
{
    public string username;
    public string password;
}

[Serializable]
public class UserResponse
{
    public UserData usuario;
    public string token;
}

[Serializable]
public class UserData
{
    public string _id;
    public string username;
    public int score;
    public ScoreData data;

    public int GetRealScore()
    {
        if (data != null && data.score > 0) return data.score;
        return score;
    }
}

[Serializable]
public class UserListResponse
{
    public List<UserData> usuarios;
}

[Serializable]
public class ScoreData
{
    public int score;
}

[Serializable]
public class PatchUserRequest
{
    public string username;
    public ScoreData data;
}

[Serializable]
public class UpdateScoreRequest
{
    public ScoreData data;
}