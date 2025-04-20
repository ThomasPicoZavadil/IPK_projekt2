PROJECT_NAME = ipk25chat-client
CONFIGURATION = Release
RUNTIME = linux-x64
OUTPUT_DIR = .
PUBLISH_DIR = $(OUTPUT_DIR)/publish

.PHONY: all clean

all: $(PROJECT_NAME)

$(PROJECT_NAME):
	dotnet publish -c $(CONFIGURATION) -r $(RUNTIME) \
		--self-contained true \
		-p:PublishSingleFile=true \
		-p:IncludeNativeLibrariesForSelfExtract=true \
		-p:PublishTrimmed=false \
		-o $(PUBLISH_DIR)

	mv $(PUBLISH_DIR)/$(PROJECT_NAME) $(OUTPUT_DIR)/$(PROJECT_NAME)
	chmod +x $(OUTPUT_DIR)/$(PROJECT_NAME)

clean:
	rm -rf $(PUBLISH_DIR)
	rm -f $(OUTPUT_DIR)/$(PROJECT_NAME)
