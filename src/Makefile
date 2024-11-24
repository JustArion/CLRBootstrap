OUT_DIR = ..\\Shared
OBJ_FILE = $(OUT_DIR)\CLRBootstrap.obj

rebuild:
	cl /EHsc /c dllmain.cpp /Fo"$(OBJ_FILE)"

build: $(OBJ_FILE)

$(OBJ_FILE): dllmain.cpp | $(OUT_DIR)
# /EHsc = C4530: C++ exception handler used, but unwind semantics are not enabled.
# /c 	= No build
	cl /EHsc /c dllmain.cpp /Fo"$(OBJ_FILE)"

$(OUT_DIR):
	mkdir $(OUT_DIR)

help:
	$(info Usage: make <target>)
	$(info )
	$(info Targets:)
	$(info  build       Build the Core Runtime Bootstrapper)
	$(info  rebuild     Build without caching)
	$(info  help        Show this help message)
	@:
