using System.Collections.Generic;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

//Code based on this tutorial: https://gist.github.com/staltz/868e7e9bc2a7b8c1f754
public class RequestTest : MonoBehaviour
{
	private class UserInfo
	{
		public string login;
		public int id;
	}

	private class UserButton
	{
		public TMP_Text title;
		public Button closeButton;
	}

	public string url = "https://api.github.com/users";
	public GameObject suggestionPrefab;
	public GameObject buttonParent;
	public int amountToShow = 3;
	public Button refreshButton;

	// Start is called before the first frame update
	void Start()
	{
		//Start of the reactive logic
		var progressNotifier = new ScheduledNotifier<float>();
		progressNotifier.Subscribe(x => Debug.Log($"Progress: {x}")); //We can pass this as a parameter to the request stream as a way to get the progress of the request

		var refreshClickStream = refreshButton.OnClickAsObservable(); //here we are creating a stream that has entries for each time we click on the refresh button

		var requestStream = refreshClickStream //We create another stream that merges a stream for when the app starts and when the click happens
			.StartWith(new Unit()) //Then we force a item to be added at the start of the stream so we can make a request at the start of the application by creating a stream with an item at the start
			.Select(_ =>
			{
				string requestURL = $"{url}?since={Random.Range(0, 500)}"; //We create a URL with a different number each time
				return requestURL;
			}); //Here we are just converting the stream event to the request URL, so we can use it later. It's literally like LINQ Select

		var responseStream = requestStream
			.SelectMany(actualURL => ObservableWWW.Get(actualURL, progress: progressNotifier)); //For each item in the stream, we create a request stream. BUT, instead of returning the actual request stream,
																								//we use "SelectMany" to return the results of all of these streams into a single stream, which in this case is the request response for each entry.

		responseStream
			.CatchIgnore((WWWErrorException exception) => //There was an error with the request
			{
				Debug.LogError($"Error: {exception.RawErrorMessage};");

				foreach (var item in exception.ResponseHeaders)
				{
					Debug.LogError(item.Key + ":" + item.Value);
				}
			})
			.Subscribe(response => //We then take the response of these requests and pass it ahead so we can process it
			{
				Debug.Log("Success!");
				//We could consume the response here, but we are gonna do it below
				//var userList = ConvertJsonToUserInfoList(response); //this is a single response. Remember, this is REACTIVE, so this will be called everytime the event is fired
			});

		//Populate the suggestions
		for (int i = 0; i < amountToShow; i++)
		{
			//Create the suggestions objects
			GameObject suggestion = Instantiate(suggestionPrefab, buttonParent.transform);
			var buttonInfo = new UserButton() {
				title = suggestion.GetComponentInChildren<TMP_Text>(),
				closeButton = suggestion.GetComponentInChildren<Button>()
			};

			//Create a stream for when clicking the close buttom
			var closeSuggestionClickStream = buttonInfo.closeButton.OnClickAsObservable();

			//We create a suggestion stream to choose which user we wanna show
			var suggestionStream = closeSuggestionClickStream
				.StartWith(new Unit()) //Since "CombineLatest" need the 2 source streams to have at least 1 entry for it to create an resulting entry, we "force" a click on the close button at the start
				.CombineLatest(responseStream, (click, response) => //In here, everytime we get a response from the response stream, we choose a random user from the list. "CombineLatest" takes the original stream + another stream and process it throught a function that takes the latest entry in both streams as parameters. We do this so we can reuse the last request response when we click the close button instead of making a new request
				{
					var userList = ConvertJsonToUserInfoList(response);
					int randomUser = Random.Range(0, userList.Count);
					return userList[randomUser];
				})
				.Merge( //We then merge the last stream with another stream that contains an empty user for when we click the refresh button, so we can show a "Loading" text or something while the request is being made
					refreshClickStream.Select(_ => new UserInfo())
				)
				.StartWith(new UserInfo()); //And just so we don't start the app with the placeholder text, we add a empty user to the start of the stream

			//And here we subscribe to the stream, "rendering" the entry
			suggestionStream.Subscribe(suggestion =>
			{
				if (suggestion.login == null)
				{
					buttonInfo.title.text = $"LOADING...";
				}
				else
				{
					buttonInfo.title.text = $"{suggestion.login}\n{suggestion.id}";
				}
			});
		}
	}

	private List<UserInfo> ConvertJsonToUserInfoList(string json)
	{
		List<UserInfo> users = new List<UserInfo>();

		//Really crude Json conversion. Don't do this.
		string[] infos = json
			.Replace("[", "")
			.Replace("]", "")
			.Split(new string[] { "}," }, System.StringSplitOptions.RemoveEmptyEntries);

		for (int i = 0; i < infos.Length; i++)
		{
			infos[i] = infos[i].Trim();
			if (!infos[i].EndsWith("}"))
			{
				infos[i] = infos[i] + "}";
			}

			UserInfo info = JsonUtility.FromJson<UserInfo>(infos[i]);
			users.Add(info);
		}

		return users;
	}

	private GameObject CreateButton(UserInfo info)
	{
		GameObject button = Instantiate(suggestionPrefab, buttonParent.transform);
		button.GetComponentInChildren<TMP_Text>().text = $"{info.login}\n{info.id}";
		return button;
	}
}
