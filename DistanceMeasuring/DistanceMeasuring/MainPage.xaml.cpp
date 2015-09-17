//
// MainPage.xaml.cpp
// Implementation of the MainPage class.
//

#include "pch.h"
#include "MainPage.xaml.h"
#include <Windows.h>
#include <thread>
#include <chrono>

using namespace DistanceMeasuring;

using namespace Platform;
using namespace Windows::Foundation;
using namespace Windows::Foundation::Collections;
using namespace Windows::UI::Xaml;
using namespace Windows::UI::Xaml::Controls;
using namespace Windows::UI::Xaml::Controls::Primitives;
using namespace Windows::UI::Xaml::Data;
using namespace Windows::UI::Xaml::Input;
using namespace Windows::UI::Xaml::Media;
using namespace Windows::UI::Xaml::Navigation;
using namespace Windows::Devices::Gpio;
using namespace concurrency;
using namespace std::literals;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

MainPage::MainPage()
{
	InitializeComponent();

   InitGPIO();
	timer = ref new DispatcherTimer();
	TimeSpan interval;
	interval.Duration = 1000 * 1000 * 10;
	timer->Interval = interval;
	timer->Tick += ref new Windows::Foundation::EventHandler<Platform::Object ^>(this, &DistanceMeasuring::MainPage::OnTick);
	if (TriggerPin != nullptr && EchoPin != nullptr)
	{
		timer->Start();
	}
}


void MainPage::InitGPIO()
{
	auto gpio = GpioController::GetDefault();
	if (gpio == nullptr)
	{
		TriggerPin = nullptr;
		EchoPin = nullptr;
		return;
	}
	

	TriggerPin = gpio->OpenPin(triggerPinNumber);
	TriggerPin->SetDriveMode(GpioPinDriveMode::Output);
	EchoPin = gpio->OpenPin(echoPinNumber);
	EchoPin->SetDriveMode(GpioPinDriveMode::Input);
	
	create_task([]() {
		Sleep(1000);
	
	}).then([this]() {
		IsReady = true;
	});

	//std::this_thread::sleep_for(2s);
}



void DistanceMeasuring::MainPage::OnTick(Platform::Object ^sender, Platform::Object ^args)
{
	GetDistance();
}

void MainPage::GetDistance()
{
	TriggerPin->Write(GpioPinValue::High);
	Sleep(0.01);
	TriggerPin->Write(GpioPinValue::Low);
	while (EchoPin->Read() == GpioPinValue::Low) {}
    
	//stepTimer.Tick();
	//auto steptimeStart= stepTimer.GetTotalSeconds();
	auto Plusestart = std::chrono::high_resolution_clock::now();
	
	while (EchoPin->Read() == GpioPinValue::High) {}

	//stepTimer.Tick();
	//auto steptimeEnd = stepTimer.GetTotalSeconds();
	auto PluseEnd= std::chrono::high_resolution_clock::now();	
	std::chrono::duration<double, std::milli> pulseLen = PluseEnd - Plusestart;
	auto distanceCM =(pulseLen.count() * 17150/1000);
	DistanceText->Text = "Time:" + pulseLen.count() / 1000 + "Distance:" + distanceCM;

	//DistanceText->Text= "Time:" + (steptimeEnd - steptimeStart) + "Distance:" + ((steptimeEnd-steptimeStart)*17150);
	

}
