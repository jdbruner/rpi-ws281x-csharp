using Native;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace rpi_ws281x
{
	/// <summary>
	/// Settings which are required to initialize the WS281x controller
	/// </summary>
	public class Settings
	{
	
		/// <summary>
		/// Settings to initialize the WS281x controller
		/// </summary>
		/// <param name="frequency">Set frequency in Hz</param>
		/// <param name="dmaChannel">Set DMA channel to use</param>
		public Settings(uint frequency = 800000, int dmaChannel = 10)
		{
			Frequency = frequency;
			DMAChannel = dmaChannel;
			Channels = new Channel[PInvoke.RPI_PWM_CHANNELS];				
		}

		/// <summary>
		/// Returns the used frequency in Hz
		/// </summary>
		public uint Frequency { get; private set; }

		/// <summary>
		/// Returns the DMA channel
		/// </summary>
		public int DMAChannel { get; private set; }

		/// <summary>
		/// Returns the channels which holds the LEDs
		/// </summary>
		public Channel[] Channels { get; private set; }
	}
}
