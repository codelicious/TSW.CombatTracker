// MouseWheelHook.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"

typedef void (CALLBACK *HookProc)(int code, WPARAM wParam, LPARAM lParam);

HINSTANCE g_appInstance = NULL;
HookProc userProc;
HHOOK mouseHook = NULL;

static LRESULT CALLBACK MouseHookLLCallback(int code, WPARAM wParam, LPARAM lParam);

extern "C" __declspec(dllexport)
void Load()
{
	// just get the dll loaded;
}

extern "C" __declspec(dllexport)
bool Register(HookProc proc)
{
	if (g_appInstance == NULL)
		return false;

	if (proc == NULL)
		return false;

	if (userProc != NULL)
		return false;

	if (mouseHook != NULL)
		return false;

	userProc = proc;
	mouseHook = ::SetWindowsHookEx(WH_MOUSE_LL, (HOOKPROC)MouseHookLLCallback, g_appInstance, 0);
	return mouseHook != NULL;
}

extern "C" __declspec(dllexport)
void Unregister()
{
	if (mouseHook != NULL)
	{
		::UnhookWindowsHookEx(mouseHook);
		mouseHook = NULL;
		userProc = NULL;
	}
}

static LRESULT CALLBACK MouseHookLLCallback(int code, WPARAM wParam, LPARAM lParam)
{
	if (code == WM_MOUSEWHEEL && mouseHook)
	{
		userProc(code, wParam, lParam);
		// return 0; // I think we want to short-circuit any more processing of wheel events or the active app would also process them
	}
		
	return ::CallNextHookEx(mouseHook, code, wParam, lParam);


}