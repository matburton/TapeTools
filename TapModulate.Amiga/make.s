IF EXISTS t:TapModulate
    delete >NIL: t:TapModulate all
ENDIF
makedir t:TapModulate 
copy >NIL: Source/#?.d t:TapModulate all
echo ""
echo "*E[33mCompiling...*E[0m"
echo ""
Draco/c/Draco t:TapModulate/Main.d t:TapModulate/Modulating.d t:TapModulate/File.d t:TapModulate/Parsing.d t:TapModulate/Timing.d
echo ""
echo "*E[33mLinking...*E[0m"
echo ""
Draco/c/Blink with blink.args
delete >NIL: t:TapModulate all
echo ""
echo "*E[33mDone*E[0m - executable built to *E[32mram:TapModulate*E[0m"
echo ""
