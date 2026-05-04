{
  description = "A .NET 10 development environment";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
    flake-utils.url = "github:numtide/flake-utils";
  };

  outputs = { self, nixpkgs, flake-utils }:
    flake-utils.lib.eachDefaultSystem (system:
      let
        pkgs = nixpkgs.legacyPackages.${system};
        dotnetPkg = (with pkgs.dotnetCorePackages; combinePackages [
          sdk_10_0
        ]);
        deps = [
          pkgs.zlib
          pkgs.zlib.dev
          pkgs.openssl
          pkgs.dotnet-ef
          dotnetPkg
        ];
      in
      {
        devShells.default = pkgs.mkShell {
          NIX_LD_LIBRARY_PATH = pkgs.lib.makeLibraryPath ([
            pkgs.stdenv.cc.cc
          ] ++ deps);
          NIX_LD = "${pkgs.stdenv.cc.libc_bin}/bin/ld.so";
          
          nativeBuildInputs = deps;
          
          shellHook = ''
            export DOTNET_ROOT="${dotnetPkg}"
          '';
        };
      }
    );
}
