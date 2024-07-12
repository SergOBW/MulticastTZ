using System;
using System.Collections;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using IngameDebugConsole;
using UnityEngine;

public class GameClient : MonoBehaviour
{
    [SerializeField] private string address = "localhost";
    [SerializeField] private int port = 5077;
    
    private static readonly HttpClient httpClient = new HttpClient();
    
    private string _baseUrl = "http://localhost:5077"; // Адрес вашего сервера
    private string _playerId;

    private void Start()
    {
        _baseUrl = $"http://{address}:{port}";
        Debug.Log(_baseUrl);
        
        StartCoroutine(AssignId());
        StartCoroutine(CheckNotifications());
        
        DebugLogConsole.AddCommand<int>( "guess", "Guess some score to server", SubmitNumberSync );
        DebugLogConsole.AddCustomParameterType( typeof( string ), ParseString );
    }

    private bool ParseString(string input, out object output)
    {
        List<string> inputSplit = new List<string>( 2 );
        DebugLogConsole.FetchArgumentsFromCommand( input, inputSplit );
        
        if( inputSplit.Count != 1 )
        {
            output = null;
            return false;
        }
        
        if( !DebugLogConsole.ParseInt( inputSplit[0], out output ) )
        {
            output = null;
            return false;
        }
        
        return true;
    }


    private IEnumerator AssignId()
    {
        yield return AssignIdAsync().AsIEnumerator();
    }

    private async Task AssignIdAsync()
    {
        var response = await httpClient.GetStringAsync($"{_baseUrl}/assignId");
        _playerId = response.Trim('\"');
        Debug.Log($"Assigned playerId: {_playerId}");

        StartCoroutine(JoinQueue());
    }

    private IEnumerator JoinQueue()
    {
        yield return JoinQueueAsync(_playerId).AsIEnumerator();
    }

    private async Task JoinQueueAsync(string playerId)
    {
        var response = await httpClient.GetStringAsync($"{_baseUrl}/join?playerId={playerId}");
        Debug.Log("Joined the queue");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            int randomNumber = Random.Range(0, 100);
            StartCoroutine(SubmitNumber(randomNumber));
        }
    }

    private void SubmitNumberSync(int number)
    {
        StartCoroutine(SubmitNumber(number));
    }
    
    private IEnumerator SubmitNumber(int number)
    {
        yield return SubmitNumberAsync(_playerId, number).AsIEnumerator();
    }

    private async Task SubmitNumberAsync(string playerId, int number)
    {
        var response = await httpClient.GetStringAsync($"{_baseUrl}/submit?playerId={playerId}&number={number}");
        Debug.Log($"Submitted number {number}");
        if (response.Contains("Waiting"))
        {
            Debug.Log("Waiting for the opponent to submit their number...");
        }
    }

    private void OnApplicationQuit()
    {
        StartCoroutine(ExitGame());
    }
    
    private IEnumerator CheckNotifications()
    {
        while (true)
        {
            yield return CheckNotificationsAsync().AsIEnumerator();
            yield return new WaitForSeconds(1); // Проверяем уведомления каждую секунду
        }
    }

    private async Task CheckNotificationsAsync()
    {
        string response = await httpClient.GetStringAsync($"{_baseUrl}/notification?playerId={_playerId}");
        if (!string.IsNullOrEmpty(response))
        {
            Debug.Log(response);
        }
    }


    private IEnumerator ExitGame()
    {
        yield return ExitGameAsync(_playerId).AsIEnumerator();
    }

    private async Task ExitGameAsync(string playerId)
    {
        var response = await httpClient.GetStringAsync($"{_baseUrl}/exit?playerId={playerId}");
        Debug.Log("Exited the game");
    }
}


public static class TaskExtensions
{
    public static IEnumerator AsIEnumerator(this Task task)
    {
        while (!task.IsCompleted)
        {
            yield return null;
        }
        if (task.Exception != null)
        {
            throw task.Exception;
        }
    }
}
