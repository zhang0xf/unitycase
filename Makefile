WORKDIR=$(shell pwd)
PROTO_PATH=$(WORKDIR)/proto
PB_CSHARP_PATH=$(WORKDIR)/Assets/Scripts/Nw/pb

default: all

all: pb

pb:
	@protoc -I=$(PROTO_PATH) --csharp_out=$(PB_CSHARP_PATH) $(PROTO_PATH)/*.proto;