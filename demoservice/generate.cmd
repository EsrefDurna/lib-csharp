@echo off
echo This demo requires babel.exe directory to be in the PATH environment variable
echo You may need to modify the path to BabelTemplates directory location below

babel -lang csharp -scopes="csharpx,cs" -client=true -model=true -templates="../../../babeltemplates"  -output="../BabelRpc.Test/gen-babel" *.babel
babel -lang csharp -scopes="csharpx,cs" -servertype="mvc" -server=true -model=true  -options="controller=BabelRpc.Mvc.BabelController" -templates="../../../babeltemplates"  -output="../BabelRpc.TestMvcService/App_Code/gen-babel" roundtrip.babel CreditCardDemo.babel
babel -lang csharp -scopes="csharpx,cs" -servertype="mvcAsync" -server=true -model=true  -options="controller=BabelRpc.Mvc.BabelController" -templates="../../../babeltemplates"  -output="../BabelRpc.TestMvcService/App_Code/gen-babel" demo.babel