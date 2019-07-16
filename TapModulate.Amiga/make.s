IF EXISTS t:TapModulate
    delete >NIL: t:TapModulate all
ENDIF
makedir t:TapModulate 
copy >NIL: Source/#?.d t:TapModulate all
echo ""
echo "*E[33mCompiling...*E[0m"
echo ""
Draco/c/Draco t:TapModulate/Entry.d t:TapModulate/Program.d t:TapModulate/File.d t:TapModulate/Parser.d
echo ""
echo "*E[33mLinking...*E[0m"
echo ""
Draco/c/Blink Draco/drlib/drstart0.o+t:TapModulate/Entry.r+t:TapModulate/Program.r+t:TapModulate/File.r+t:TapModulate/Parser.r lib Draco/drlib/exec.lib+Draco/drlib/dos.lib+Draco/drlib/exec.lib+Draco/drlib/drio.lib+Draco/drlib/draco.lib to ram:TapModulate smallcode
delete >NIL: t:TapModulate all
echo ""
echo "*E[33mDone*E[0m - executable built to *E[32mram:TapModulate*E[0m"
echo ""
