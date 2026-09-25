typedef unsigned char BYTE;
typedef unsigned short WORD;
typedef unsigned int DWORD;
typedef int BOOL;
typedef unsigned int UINT;
typedef unsigned long long ULONG_PTR;
typedef unsigned long long SIZE_T;

void *memcpy(void *dst, const void *src, SIZE_T n) { unsigned char *d=(unsigned char*)dst; const unsigned char *s=(const unsigned char*)src; while(n--) *d++=*s++; return dst; }
void *memset(void *dst, int c, SIZE_T n) { unsigned char *d=(unsigned char*)dst; while(n--) *d++=(unsigned char)c; return dst; }

typedef void *HANDLE;
typedef void *HMODULE;
typedef unsigned short WCHAR;
typedef WCHAR *LPWSTR;
typedef const WCHAR *LPCWSTR;
typedef void *LPVOID;
typedef const void *LPCVOID;

#define NULL ((void*)0)
#define INVALID_FILE_ATTRIBUTES ((DWORD)0xFFFFFFFFu)
#define STARTF_USESHOWWINDOW 0x00000001u
#define STD_ERROR_HANDLE ((DWORD)-12)

__declspec(dllimport) DWORD __stdcall GetModuleFileNameW(HMODULE, LPWSTR, DWORD);
__declspec(dllimport) DWORD __stdcall GetFileAttributesW(LPCWSTR);
__declspec(dllimport) LPWSTR __stdcall GetCommandLineW(void);
__declspec(dllimport) HANDLE __stdcall GetStdHandle(DWORD);
__declspec(dllimport) BOOL __stdcall WriteConsoleW(HANDLE, LPCVOID, DWORD, DWORD*, LPVOID);
__declspec(dllimport) void __stdcall ExitProcess(UINT);
__declspec(dllimport) BOOL __stdcall CloseHandle(HANDLE);

typedef struct _STARTUPINFOW {
    DWORD cb;
    LPWSTR lpReserved;
    LPWSTR lpDesktop;
    LPWSTR lpTitle;
    DWORD dwX;
    DWORD dwY;
    DWORD dwXSize;
    DWORD dwYSize;
    DWORD dwXCountChars;
    DWORD dwYCountChars;
    DWORD dwFillAttribute;
    DWORD dwFlags;
    WORD wShowWindow;
    WORD cbReserved2;
    BYTE *lpReserved2;
    HANDLE hStdInput;
    HANDLE hStdOutput;
    HANDLE hStdError;
} STARTUPINFOW;

typedef struct _PROCESS_INFORMATION {
    HANDLE hProcess;
    HANDLE hThread;
    DWORD dwProcessId;
    DWORD dwThreadId;
} PROCESS_INFORMATION;

__declspec(dllimport) BOOL __stdcall CreateProcessW(
    LPCWSTR lpApplicationName,
    LPWSTR lpCommandLine,
    LPVOID lpProcessAttributes,
    LPVOID lpThreadAttributes,
    BOOL bInheritHandles,
    DWORD dwCreationFlags,
    LPVOID lpEnvironment,
    LPCWSTR lpCurrentDirectory,
    STARTUPINFOW *lpStartupInfo,
    PROCESS_INFORMATION *lpProcessInformation);

static WCHAR launcher_path[32768];
static WCHAR game_path[32768];
static WCHAR command_line[65536];

static DWORD wlen(const WCHAR *s) {
    DWORD n = 0;
    while (s[n]) n++;
    return n;
}

static void write_error(const WCHAR *s) {
    DWORD written = 0;
    HANDLE h = GetStdHandle(STD_ERROR_HANDLE);
    if (h) WriteConsoleW(h, s, wlen(s), &written, NULL);
}

static int is_space(WCHAR c) {
    return c == L' ' || c == L'\t';
}

static WCHAR lower_ascii(WCHAR c) {
    if (c >= L'A' && c <= L'Z') return (WCHAR)(c + (L'a' - L'A'));
    return c;
}

static int arg_equals(const WCHAR *start, DWORD len, const WCHAR *word) {
    DWORD i = 0;
    while (word[i]) {
        if (i >= len || lower_ascii(start[i]) != lower_ascii(word[i])) return 0;
        i++;
    }
    return i == len;
}

static const WCHAR *skip_program_name(const WCHAR *p) {
    if (*p == L'\"') {
        p++;
        while (*p && *p != L'\"') p++;
        if (*p == L'\"') p++;
    } else {
        while (*p && !is_space(*p)) p++;
    }
    while (is_space(*p)) p++;
    return p;
}

static int parse_width(const WCHAR **pp, int *width) {
    const WCHAR *p = *pp;
    int value = 0;
    int digits = 0;
    while (*p >= L'0' && *p <= L'9') {
        value = value * 10 + (int)(*p - L'0');
        p++;
        digits++;
        if (value > 100000) return 0;
    }
    if (!digits || (*p && !is_space(*p))) return 0;
    while (is_space(*p)) p++;
    *pp = p;
    *width = value;
    return 1;
}

static DWORD append_text(WCHAR *dst, DWORD pos, DWORD cap, const WCHAR *src) {
    while (*src && pos + 1 < cap) dst[pos++] = *src++;
    dst[pos] = 0;
    return pos;
}

static DWORD append_uint(WCHAR *dst, DWORD pos, DWORD cap, unsigned int value) {
    WCHAR tmp[16];
    DWORD n = 0;
    do {
        tmp[n++] = (WCHAR)(L'0' + (value % 10));
        value /= 10;
    } while (value && n < 16);
    while (n && pos + 1 < cap) dst[pos++] = tmp[--n];
    dst[pos] = 0;
    return pos;
}

void mainCRTStartup(void) {
    DWORD n = GetModuleFileNameW(NULL, launcher_path, 32768);
    if (!n || n >= 32767) {
        write_error(L"Unable to determine launcher directory.\r\n");
        ExitProcess(1);
    }

    DWORD slash = n;
    while (slash > 0 && launcher_path[slash - 1] != L'\\' && launcher_path[slash - 1] != L'/') slash--;
    if (slash == 0) {
        write_error(L"Unable to determine launcher directory.\r\n");
        ExitProcess(1);
    }
    DWORD dir_len = slash;
    if (!(dir_len == 3 && launcher_path[1] == L':')) dir_len--;
    WCHAR saved = launcher_path[dir_len];
    launcher_path[dir_len] = 0;

    DWORD pos = 0;
    while (launcher_path[pos] && pos + 1 < 32768) {
        game_path[pos] = launcher_path[pos];
        pos++;
    }
    if (pos && game_path[pos - 1] != L'\\' && game_path[pos - 1] != L'/') game_path[pos++] = L'\\';
    const WCHAR game_name[] = L"AKAIITO_HD_REMASTER.exe";
    DWORD gi = 0;
    while (game_name[gi] && pos + 1 < 32768) game_path[pos++] = game_name[gi++];
    game_path[pos] = 0;

    if (GetFileAttributesW(game_path) == INVALID_FILE_ATTRIBUTES) {
        write_error(L"Could not find AKAIITO_HD_REMASTER.exe beside AKAIITO4x3Launcher.exe.\r\n");
        launcher_path[dir_len] = saved;
        ExitProcess(1);
    }

    const WCHAR *p = skip_program_name(GetCommandLineW());
    int width = 0;
    if (!parse_width(&p, &width) || width < 640 || width > 7680) {
        write_error(L"Usage: AKAIITO4x3Launcher.exe <width> [windowed|fullscreen]\r\n");
        launcher_path[dir_len] = saved;
        ExitProcess(2);
    }

    int fullscreen = 0;
    if (*p) {
        const WCHAR *start = p;
        while (*p && !is_space(*p)) p++;
        DWORD len = (DWORD)(p - start);
        fullscreen = arg_equals(start, len, L"fullscreen") || arg_equals(start, len, L"full");
    }

    unsigned int height = (unsigned int)((width * 3 + 2) / 4);
    if (height & 1u) height++;

    pos = 0;
    pos = append_text(command_line, pos, 65536, L"\"");
    pos = append_text(command_line, pos, 65536, game_path);
    pos = append_text(command_line, pos, 65536, L"\" -screen-width ");
    pos = append_uint(command_line, pos, 65536, (unsigned int)width);
    pos = append_text(command_line, pos, 65536, L" -screen-height ");
    pos = append_uint(command_line, pos, 65536, height);
    pos = append_text(command_line, pos, 65536, L" -screen-fullscreen ");
    pos = append_uint(command_line, pos, 65536, fullscreen ? 1u : 0u);

    STARTUPINFOW si = {0};
    PROCESS_INFORMATION pi = {0};
    si.cb = (DWORD)sizeof(si);

    if (!CreateProcessW(game_path, command_line, NULL, NULL, 0, 0, NULL, launcher_path, &si, &pi)) {
        write_error(L"Failed to launch AKAIITO_HD_REMASTER.exe.\r\n");
        launcher_path[dir_len] = saved;
        ExitProcess(1);
    }

    CloseHandle(pi.hThread);
    CloseHandle(pi.hProcess);
    launcher_path[dir_len] = saved;
    ExitProcess(0);
}
