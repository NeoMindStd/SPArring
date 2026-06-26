#include <BWAPI.h>
#include <Windows.h>

#include "NeoTerranF.h"

extern "C" __declspec(dllexport) void gameInit(BWAPI::Game* game)
{
    BWAPI::BroodwarPtr = game;
}

BOOL APIENTRY DllMain(HANDLE, DWORD, LPVOID)
{
    return TRUE;
}

extern "C" __declspec(dllexport) BWAPI::AIModule* newAIModule()
{
    return new NeoTerranF();
}
