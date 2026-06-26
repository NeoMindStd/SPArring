#include "NeoZergF.h"

extern "C" __declspec(dllexport) BWAPI::AIModule* newAIModule()
{
    return new NeoZergF();
}

extern "C" __declspec(dllexport) void gameInit(BWAPI::Game* game)
{
    BWAPI::BroodwarPtr = game;
}
