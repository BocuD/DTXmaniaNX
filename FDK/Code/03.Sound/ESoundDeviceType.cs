﻿namespace FDK;

public enum ESoundDeviceType : int
{
	DirectSound = 0,
	SharedWASAPI = 1,
	ExclusiveWASAPI = 2,
	ASIO = 3,
	Unknown = 4,
}