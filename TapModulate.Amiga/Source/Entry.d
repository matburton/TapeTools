#Draco/drinc/exec/miscellaneous.g
#Draco/drinc/exec/tasks.g
#Draco/drinc/exec/ports.g
#Draco/drinc/libraries/dos.g
#Draco/drinc/libraries/dosextens.g
#Draco/drinc/workbench/startup.g
#Includes/Program.g

extern _d_IO_initialize() void;

proc main() void:
    *Process_t pProcess;
    *WBStartup_t pWBStartup;
    *DosLibrary_t pDosLibrary;
    pWBStartup := nil;
    if OpenExecLibrary(0) ~= nil then
        pProcess := pretend(FindTask(nil), *Process_t);
        if pretend(pProcess*.pr_CLI, arbptr) = nil then
            ignore(WaitPort(&pProcess*.pr_MsgPort));
            pWBStartup := pretend(GetMsg(&pProcess*.pr_MsgPort), *WBStartup_t);      
        fi;
        pDosLibrary := OpenDosLibrary(0);
        if pDosLibrary ~= nil then
            _d_IO_initialize();
            Start();
            CloseDosLibrary();
        fi;
        if pWBStartup ~= nil then           
            Forbid();
            ReplyMsg(pretend(pWBStartup, *Message_t));
        fi;
        CloseExecLibrary();
    fi;
corp;