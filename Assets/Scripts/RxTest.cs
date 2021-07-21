using System;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

public class RxTest : MonoBehaviour
{
	private void Start()
	{
		// An "Observable" is a Data Stream, which is a sequence of events over time. It's like a collection of objects over time. Imagine a list with objects that get added over time. These objects can be almost anything, like variables and even events. Every time a value/event happens, anything subscribed to the stream will be called
		var clickStream = this.UpdateAsObservable() //In this case, we create a data stream based on the "Update" event from Unity. This creates a data stream for every update (in this case, we have no data aside from the Default "Unit", it's just fired every update).
			.Where(_ => Input.GetMouseButtonDown(0))
			.Select(_ => Input.mousePosition); //Since this will be fired every update, we can then filter only the ones where a MouseClick happened (basically, since this is firing in realtime, we can check if the mouse button was pressed on the same frame this event was fired)

		clickStream.Buffer(clickStream.Throttle(TimeSpan.FromMilliseconds(250))) //Here we are creating a data stream that contains lists of click events, where every list contains the clicks that happened below 250 milliseconds from each other
			.Where(clicks => clicks.Count >= 2) //We then filter these lists to only the ones that contains more than 2 clicks
			.Subscribe(doubleClicks => Debug.Log($"Double click detected! Count: {doubleClicks.Count}")); //And finally, we subscribe to it, saying what we gonna do with the result of all these filterings. In this case, we just log saying we detected a double click

		clickStream.Subscribe(clickPos => Debug.Log($"A click was detected on position: {clickPos}"));
	}
}
