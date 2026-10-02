using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase;
using Firebase.Auth;
using Firebase.Database;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance;

    [Header("UI Panels & Containers")]
    public GameObject panelAuth;
    public GameObject panelUser;
    public GameObject gameContainer;

    [Header("Input Fields - Auth")]
    public TMP_InputField inputNombre; // Usar como correo
    public TMP_InputField inputPassword;
    public TMP_InputField inputDatosAdicionales; // Requerido por la actividad

    [Header("Input Fields - Game/User")]
    public TMP_Text textWelcome;
    public TMP_Text textLeaderboard;

    private FirebaseAuth auth;
    private DatabaseReference db;

    private readonly Queue<Action> _executeOnMainThread = new Queue<Action>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        panelAuth.SetActive(true);
        panelUser.SetActive(false);
        if (gameContainer != null) gameContainer.SetActive(false);

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(t => {
            if (t.Result == DependencyStatus.Available) {
                auth = FirebaseAuth.DefaultInstance;
                db = FirebaseDatabase.DefaultInstance.RootReference;
                Debug.Log("Firebase inicializado y listo."); // <-- Mensaje de éxito
            } else {
                Debug.LogError("Error al cargar Firebase: " + t.Result); // <-- Mensaje de error
            }
        });
    }

    public void OnClickStartGame()
    {
        if (panelUser != null) panelUser.SetActive(false);
        if (gameContainer != null) gameContainer.SetActive(true);
    }

    public void OnClickLogout()
    {
        auth.SignOut();
        panelAuth.SetActive(true);
        panelUser.SetActive(false);
        if (gameContainer != null) gameContainer.SetActive(false);
    }

    public void SubmitGameScore(int finalScore)
    {
        if (panelUser != null) panelUser.SetActive(true);
        if (gameContainer != null) gameContainer.SetActive(false);
        GuardarRecord(finalScore);
    }

    public void OnClickRegister()
    {
        string email = inputNombre.text.Trim();
        string pass = inputPassword.text.Trim();
        string username = email.Split('@')[0]; 
        string extra = inputDatosAdicionales != null ? inputDatosAdicionales.text : "Sin datos";

        auth.CreateUserWithEmailAndPasswordAsync(email, pass).ContinueWith(t => {
            if (t.IsFaulted) { 
                // ESTO TE DIRÁ EXACTAMENTE POR QUÉ NO TE DEJA REGISTRARTE
                Debug.LogError("❌ Error al registrar: " + t.Exception.Flatten().InnerExceptions[0].Message); 
                return;
            }
            
            string uid = t.Result.User.UserId;
            db.Child("users").Child(uid).Child("username").SetValueAsync(username);
            db.Child("users").Child(uid).Child("extra").SetValueAsync(extra);
            db.Child("users").Child(uid).Child("score").SetValueAsync(0);
            
            Debug.Log("✅ ¡Registro exitoso en Firebase!");
            UnityMainThreadDispatcher(() => LoginExitoso(username)); 
        });
    }

    public void OnClickLogin()
    {
        string email = inputNombre.text.Trim();
        string pass = inputPassword.text.Trim();

        auth.SignInWithEmailAndPasswordAsync(email, pass).ContinueWith(t => {
            if (t.IsFaulted) {
                Debug.LogError("❌ Error de Login: " + t.Exception.Flatten().InnerExceptions[0].Message);
                return;
            }
            
            Debug.Log("✅ ¡Login exitoso!");
            string username = email.Split('@')[0];
            UnityMainThreadDispatcher(() => LoginExitoso(username));
        });
    }

    private void LoginExitoso(string username)
    {
        if (textWelcome != null) textWelcome.text = "Bienvenid@ " + username;
        panelAuth.SetActive(false);
        panelUser.SetActive(true);
        ActualizarLeaderboard();
    }

    private void GuardarRecord(int nuevoScore)
    {
        if (auth.CurrentUser == null) return;
        string uid = auth.CurrentUser.UserId;
        
        db.Child("users").Child(uid).Child("score").GetValueAsync().ContinueWith(t => {
            if (t.IsFaulted) return;
            int actual = t.Result.Value != null ? Convert.ToInt32(t.Result.Value) : 0;
            
            if (nuevoScore > actual) {
                db.Child("users").Child(uid).Child("score").SetValueAsync(nuevoScore);
                string username = auth.CurrentUser.Email.Split('@')[0];
                db.Child("leaderboard").Child(uid).UpdateChildrenAsync(new Dictionary<string, object> {
                    {"name", username}, 
                    {"score", nuevoScore}
                });
            }
            UnityMainThreadDispatcher(ActualizarLeaderboard);
        });
    }

    private void ActualizarLeaderboard()
    {
        db.Child("leaderboard").OrderByChild("score").LimitToLast(10).GetValueAsync().ContinueWith(t => {
            if (t.IsFaulted) return;
            List<string> items = new List<string>();
            foreach (var child in t.Result.Children) {
                items.Add($"{child.Child("name").Value}: {child.Child("score").Value}");
            }
            items.Reverse(); 
            string tabla = string.Join("\n", items);
            UnityMainThreadDispatcher(() => {
                if (textLeaderboard != null) textLeaderboard.text = tabla;
            });
        });
    }

    private void UnityMainThreadDispatcher(Action action)
    {
        lock (_executeOnMainThread)
        {
            _executeOnMainThread.Enqueue(action);
        }
    }

    void Update()
    {
        lock (_executeOnMainThread)
        {
            while (_executeOnMainThread.Count > 0)
            {
                _executeOnMainThread.Dequeue().Invoke();
            }
        }
    }
}