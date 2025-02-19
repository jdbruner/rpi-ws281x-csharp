﻿using Native;
using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;

/// TODO: rewrite this wrapper class
/// The native structure has an array that isn't blittable, requiring various accommodations.
/// Beyond that the wrapper needs some general cleanup
///
/// Note that this requires the native library to be present, and must run as root due
/// to the requirement to use mmap()

namespace rpi_ws281x
{
	/// <summary>
	/// Wrapper class to controll WS281x LEDs
	/// </summary>
	public class WS281x : IDisposable
	{
		private ws2811_t _ws2811;
		private GCHandle _ws2811Handle;
		private bool _isDisposingAllowed;

		/// <summary>
		/// Initialize the wrapper
		/// </summary>
		/// <param name="settings">Settings used for initialization</param>
		public WS281x(Settings settings)
		{
			if (PInvoke.geteuid() != 0)
				throw new Exception("root access required to use WS281x");

			_ws2811 = new ws2811_t();
			//Pin the object in memory. Otherwise GC will probably move the object to another memory location.
			//This would cause errors because the native library has a pointer on the memory location of the object.
			_ws2811Handle = GCHandle.Alloc(_ws2811, GCHandleType.Pinned);

			_ws2811.dmanum	= settings.DMAChannel;
			_ws2811.freq	= settings.Frequency;
			_ws2811.channel0= new();
			_ws2811.channel1= new();


			for (int i=0; i< 2; i++)
				if(settings.Channels[i] != null)
					InitChannel(i, settings.Channels[i]);

			Settings = settings;

			var initResult = PInvoke.ws2811_init(ref _ws2811);
			if (initResult != ws2811_return_t.WS2811_SUCCESS)
			{
				var returnMessage = GetMessageForStatusCode(initResult);
				throw new Exception(String.Format("Error while initializing.{0}Error code: {1}{0}Message: {2}", Environment.NewLine, initResult.ToString(), returnMessage));
			}	

			//Disposing is only allowed if the init was successfull.
			//Otherwise the native cleanup function throws an error.
			_isDisposingAllowed = true;
		}

		/// <summary>
		/// Renders the content of the channels
		/// </summary>
		public void Render()
		{
			for(int i=0; i<= Settings.Channels.Length -1; i++)
			{
				if (Settings.Channels[i] != null)
				{
					var ledColor = Settings.Channels[i].LEDs.Select(x => x.RGBValue).ToArray();
					ref ws2811_channel_t channel = ref _ws2811.channel0;
					if (i == 1) channel = ref _ws2811.channel1;
					Marshal.Copy(ledColor, 0, channel.leds, ledColor.Count());
				}
			}
			
			var result = PInvoke.ws2811_render(ref _ws2811);
			if (result != ws2811_return_t.WS2811_SUCCESS)
			{
				var returnMessage = GetMessageForStatusCode(result);
				throw new Exception(String.Format("Error while rendering.{0}Error code: {1}{0}Message: {2}", Environment.NewLine, result.ToString(), returnMessage));
			}
		}

		/// <summary>
		/// Sets the color of a given LED
		/// </summary>
		/// <param name="channelIndex">Channel which controls the LED</param>
		/// <param name="ledID">ID/Index of the LED</param>
		/// <param name="color">New color</param>
		public void SetLEDColor(int channelIndex, int ledID, Color color)
		{
			Settings.Channels[channelIndex].LEDs[ledID].Color = color;
		}

		/// <summary>
		/// Set all LEDs in a specified channel to the same color
		/// </summary>
		/// <param name="channelIndex">Channel which controls the LED</param>
		/// <param name="color">New color</param>
		public void SetAllLEDColor(int channelIndex, Color color)
		{
			for (int i = 0; i < Settings.Channels[channelIndex].LEDs.Count; i++)
				Settings.Channels[channelIndex].LEDs[i].Color = color;
		}

		/// <summary>
		/// Returns the settings which are used to initialize the component
		/// </summary>
		public Settings Settings { get; private set; }

		/// <summary>
		/// Initialize the channel propierties
		/// </summary>
		/// <param name="channelIndex">Index of the channel tu initialize</param>
		/// <param name="channelSettings">Settings for the channel</param>
		private void InitChannel(int channelIndex, Channel channelSettings)
		{
            ref ws2811_channel_t channel = ref _ws2811.channel0;
            if (channelIndex == 1) channel = ref _ws2811.channel1;
            channel.count = channelSettings.LEDs.Count;
            channel.gpionum		= channelSettings.GPIOPin;
			channel.brightness	= channelSettings.Brightness;
			channel.invert		= Convert.ToInt32(channelSettings.Invert);

			if(channelSettings.StripType != StripType.Unknown)
			{
				//Strip type is set by the native assembly if not explicitly set.
				//This type defines the ordering of the colors e. g. RGB or GRB, ...
				channel.strip_type = (int)channelSettings.StripType;
			}
		}

		/// <summary>
		/// Returns the error message for the given status code
		/// </summary>
		/// <param name="statusCode">Status code to resolve</param>
		/// <returns></returns>
		private string GetMessageForStatusCode(ws2811_return_t statusCode)
		{
			var strPointer = PInvoke.ws2811_get_return_t_str((int)statusCode);
			return Marshal.PtrToStringAuto(strPointer) ?? "";
		}

	#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects).
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				if(_isDisposingAllowed)
				{
					PInvoke.ws2811_fini(ref _ws2811);
					_ws2811Handle.Free();
										
					_isDisposingAllowed = false;
				}
				
				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		~WS281x()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(false);
		}

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
	#endregion
	}
}
