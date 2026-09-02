using System;
using System.Collections.Generic;

[Serializable]
public class AuthData
{
    public string username; // O "nombre" según la API
    public string email;    // O "correo"
    public string password;
}

[Serializable]
public class AuthResponse
{
    public string token;
    public UserData usuario;
}

[Serializable]
public class UserData
{
    public string _id;
    public string username;
    public int score;
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