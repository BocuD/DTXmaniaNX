using SlimDXKey = SlimDX.DirectInput.Key;

namespace FDK;

public interface IInputKeyboard : IInputDevice
{
	//set while ImGui or a text field has the keyboard
	bool preventKeyboardInput { get; set; }

	bool bKeyPressed(SlimDXKey key) => bKeyPressed((int)key);
	bool bKeyPressing(SlimDXKey key) => bKeyPressing((int)key);
	bool bKeyReleased(SlimDXKey key) => bKeyReleased((int)key);
	bool bKeyReleasing(SlimDXKey key) => bKeyReleasing((int)key);
}
