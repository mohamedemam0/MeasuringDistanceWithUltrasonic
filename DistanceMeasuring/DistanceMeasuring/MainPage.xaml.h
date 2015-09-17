//
// MainPage.xaml.h
// Declaration of the MainPage class.
//

#pragma once

#include "MainPage.g.h"
#include "StepTimer.h"


namespace DistanceMeasuring
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public ref class MainPage sealed
	{
	public:
		MainPage();


	private:
		void InitGPIO();
		void GetDistance();
		void OnTick(Platform::Object ^sender, Platform::Object ^args);
		bool IsReady;
		const int triggerPinNumber = 18;
		Windows::Devices::Gpio::GpioPin^ TriggerPin;
		const int echoPinNumber = 23;
		Windows::Devices::Gpio::GpioPin^ EchoPin;
		Windows::UI::Xaml::DispatcherTimer^ timer;
		StepTimer stepTimer;
		
	};
}
