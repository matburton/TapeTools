#Draco/drinc/libraries/dos.g
#Draco/drinc/util.g
#Includes/File.g

proc GetByteCount(Handle_t fileHandle; *long pByteCount) bool:
    long previousPosition;
    previousPosition := Seek(fileHandle, 0, OFFSET_END);
    if previousPosition = -1 then
        return false;
    fi;
    pByteCount* := Seek(fileHandle, previousPosition, OFFSET_BEGINNING);
    if pByteCount* = -1 then
        return false;
    fi;
    true
corp;

proc ReadFile(Handle_t fileHandle; *ByteArray_t pByteArray) bool:
    long byteCount;
    *byte pBytes;
    if not GetByteCount(fileHandle, &byteCount) then
        return false;
    fi;
    pBytes := Malloc(byteCount);
    if pBytes = nil then
        return false;
    fi;
    if Read(fileHandle, pBytes, byteCount)~= byteCount then
        Mfree(pBytes, byteCount);
        return false;
    fi;
    pByteArray*.ba_pBytes := pBytes;
    pByteArray*.ba_byteCount := byteCount;
    true
corp;

proc LoadFile(*char pPath; *ByteArray_t pByteArray) bool:
    Handle_t fileHandle;
    bool result;
    fileHandle := Open(pPath, MODE_READONLY);
    if fileHandle = 0 then
        return false;
    fi;
    result := ReadFile(fileHandle, pByteArray);
    Close(fileHandle);
    result
corp;