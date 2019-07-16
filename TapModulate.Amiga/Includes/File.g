
type ByteArray_t = struct
{
    *byte ba_pBytes;
    long  ba_byteCount;
};

/* Populates pByteArray with the contents of the file */
/* The caller is responsible for deallocating pBytes */
/* Returns false if the file couldn't be loaded for any reason */
extern LoadFile(*char pPath; *ByteArray_t pByteArray) bool;