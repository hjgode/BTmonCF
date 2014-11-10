// btmonCPP.cpp : Defines the entry point for the DLL application.
//

#include "stdafx.h"

extern "C" {
    __declspec(dllexport) void __cdecl SimulateGameDLL (int a, int b);
}

// The keywords and parameter types must match the above extern
// declaration.
extern void __cdecl SimulateGameDLL (int num_games, int rand_in) {

    // This is part of the DLL, so we can call any function we want
    // in the C++. The parameters can have any names we want to give
    // them and they don't need to match the extern declaration.
}

BOOL APIENTRY DllMain( HANDLE hModule, 
                       DWORD  ul_reason_for_call, 
                       LPVOID lpReserved
					 )
{
    switch (ul_reason_for_call)
    {
        case DLL_PROCESS_ATTACH:
        case DLL_THREAD_ATTACH:
        case DLL_THREAD_DETACH:
        case DLL_PROCESS_DETACH:
            break;
    }
	return TRUE;
}

