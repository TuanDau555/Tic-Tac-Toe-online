using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Init : MonoBehaviour
{
    async void Start()
    {
        // Initialize the Unity Services SDK (required for Authentication)
        await UnityServices.InitializeAsync();

        // If is Initialized, we can sign in the player
        if (UnityServices.State == ServicesInitializationState.Initialized)
        {
            // Subscribe to the SignedIn event before signing in
            AuthenticationService.Instance.SignedIn += OnSignedIn;

            // Sign in the player anonymously
            await SignInAnonymouslyAsync();
        }
    }

    // Prevent closing the game while the player is signed in
    async Task SignInAnonymouslyAsync()
    {
        try
        {
            // The player doesn't have an account, so we create one
            // No third-party login (e.g. Google, Facebook) is required
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Signed in anonymously");

        }
        // Authentication Error
        catch (AuthenticationException e)
        {
            Debug.LogException(e);
        }
        // Request Failed Error (e.g. network error, server down)
        catch (RequestFailedException e)
        {
            Debug.LogException(e);
        }

        // If the player is already signed in, we load the main menu
        if (AuthenticationService.Instance.IsSignedIn)
        {
            // string playerName = PlayerPrefs.GetString("PlayerName");
            // if (playerName == "")
            // {
            //     // If the player name is empty, we set it to a default value
            //     playerName = "Player" + AuthenticationService.Instance.PlayerId;
            //     PlayerPrefs.SetString("PlayerName", playerName);
            // }

            SceneManager.LoadSceneAsync("Main Menu");
        }
    }

    private void OnSignedIn()
    {
        Debug.Log("Token: " + AuthenticationService.Instance.AccessToken);
        Debug.Log("Player ID: " + AuthenticationService.Instance.PlayerId);
    }
    
}
