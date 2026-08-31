using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HttpHandler : MonoBehaviour
{
    private readonly HttpListener http = new();
    private Task listen;
    private string request = "";


    private void Start()
    {
        SceneManager.LoadScene(1);
        http.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
        http.Prefixes.Add("http://127.0.0.1:8013/");
        http.Start();
        listen = new Task(httpListen);
        listen.Start();
        print("server started");
    }

    private void Update()
    {
        if (request == "") return;
        var data = JsonConvert.DeserializeObject<EditRequestjson>(request);

        var loader = GameObject.Find("DataLoader").GetComponent<JsonDataLoader>();
        var timeProvider = GameObject.Find("AudioTimeProvider").GetComponent<AudioTimeProvider>();
        var bgManager = GameObject.Find("Background").GetComponent<BGManager>();
        var bgCover = GameObject.Find("BackgroundCover").GetComponent<SpriteRenderer>();
        var screenRecorder = GameObject.Find("ScreenRecorder").GetComponent<ScreenRecorder>();
        var multTouchHandler = GameObject.Find("MultTouchHandler").GetComponent<MultTouchHandler>();
        var objectCounter = GameObject.Find("ObjectCounter").GetComponent<ObjectCounter>();

        if (data.control == EditorControlMethod.Start)
        {
            request = "";
            timeProvider.SetStartTime(data.startAt, data.startTime, data.audioSpeed);

            // start async video load first
            bgManager.LoadBGFromPath(data.maidataPath, data.audioSpeed, data.moviePath);
            bgCover.color = new Color(0f, 0f, 0f, data.backgroundCover);

            // then load chart
            loader.noteSpeed = (float)(107.25 / (71.4184491 * Mathf.Pow(data.noteSpeed + 0.9975f, -0.985558604f)));
            loader.touchSpeed = data.touchSpeed;
            loader.smoothSlideAnime = data.smoothSlideAnime;
            objectCounter.ComboSetActive(data.comboStatusType);
            loader.LoadJson(File.ReadAllText(data.jsonPath), data.startTime);
            GameObject.Find("Notes").GetComponent<PlayAllPerfect>().enabled = false;
            GameObject.Find("MultTouchHandler").GetComponent<MultTouchHandler>().clearSlots();
        }

        if (data.control == EditorControlMethod.OpStart)
        {
            request = "";
            timeProvider.SetStartTime(data.startAt, data.startTime, data.audioSpeed);
            loader.noteSpeed = (float)(107.25 / (71.4184491 * Mathf.Pow(data.noteSpeed + 0.9975f, -0.985558604f)));
            loader.touchSpeed = data.touchSpeed;
            loader.smoothSlideAnime = data.smoothSlideAnime;
            objectCounter.ComboSetActive(data.comboStatusType);
            loader.LoadJson(File.ReadAllText(data.jsonPath), data.startTime);
            GameObject.Find("MultTouchHandler").GetComponent<MultTouchHandler>().clearSlots();

            bgManager.LoadBGFromPath(data.maidataPath, data.audioSpeed, data.moviePath);
            bgCover.color = new Color(0f, 0f, 0f, data.backgroundCover);
            bgManager.PlaySongDetail();
        }

        if (data.control == EditorControlMethod.Record)
        {
            request = "";
            timeProvider.SetStartTime(data.startAt, data.startTime, data.audioSpeed, true);
            loader.noteSpeed = (float)(107.25 / (71.4184491 * Mathf.Pow(data.noteSpeed + 0.9975f, -0.985558604f)));
            loader.touchSpeed = data.touchSpeed;
            loader.smoothSlideAnime = data.smoothSlideAnime;
            objectCounter.ComboSetActive(data.comboStatusType);
            loader.LoadJson(File.ReadAllText(data.jsonPath), data.startTime);
            multTouchHandler.clearSlots();

            screenRecorder.CutoffTime = getChartLength();
            screenRecorder.CutoffTime += 10f;
            screenRecorder.StartRecording(data.maidataPath);

            bgManager.LoadBGFromPath(data.maidataPath, data.audioSpeed, data.moviePath);
            bgCover.color = new Color(0f, 0f, 0f, data.backgroundCover);
            bgManager.PlaySongDetail();
            GameObject.Find("CanvasButtons").SetActive(false);
        }

        if (data.control == EditorControlMethod.Pause)
        {
            timeProvider.isStart = false;
            bgManager.PauseVideo();

            if (data.appPort != -1)
            {
                var responseData = new EditRequestjson
                {
                    control = EditorControlMethod.Pause,
                    startTime = timeProvider.AudioTime
                };
                BroadcastToApp(JsonConvert.SerializeObject(responseData), data.appPort);
            }
        }

        if (data.control == EditorControlMethod.Stop)
        {
            screenRecorder.StopRecording();
            timeProvider.ResetStartTime();
            SceneManager.LoadScene(1);
        }

        if (data.control == EditorControlMethod.Continue)
        {
            timeProvider.SetStartTime(data.startAt, data.startTime, data.audioSpeed);
            bgManager.ContinueVideo(data.audioSpeed);
        }

        request = "";
    }

    private void OnDestroy()
    {
        http.Stop();
        print("server stoped");
    }

    private void httpListen()
    {
        while (http.IsListening)
        {
            var context = http.GetContext();
            print(context.Request.HttpMethod);
            var reader = new StreamReader(context.Request.InputStream);
            var data = reader.ReadToEnd();
            print(data);
            request = data;
            while (request != "") ;
            context.Response.StatusCode = 200;
            var stream = new StreamWriter(context.Response.OutputStream);
            stream.WriteLine("Hello!!!");
            stream.Close();
            context.Response.Close();
        }

        print("exit listen");
    }

    private float getChartLength()
    {
        var length = 0f;
        foreach (var noteData in GameObject.Find("Notes").GetComponentsInChildren<NoteDrop>(true))
        {
            length = Math.Max(length, noteData.time);

            var longData = noteData as NoteLongDrop;
            if (longData != null) length = Math.Max(length, noteData.time + longData.LastFor);
        }

        return length;
    }

    private void BroadcastToApp(string json, int port)
    {
        Task.Run(() =>
        {
            try
            {
                using (var udpClient = new UdpClient())
                {
                    var bytes = Encoding.UTF8.GetBytes(json);
                    var endpoint = new IPEndPoint(IPAddress.Loopback, port);
                    udpClient.Send(bytes, bytes.Length, endpoint);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MajdataView] Failed to send to App: {ex.Message}");
            }
        });
    }
}