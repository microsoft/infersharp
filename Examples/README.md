This directory contains a small example to play with Infer#. It includes some simple programming errors 
that is caught by Infer#.

Try this example by running
   ```bash
      # Build csharp_hello
      cd csharp_hello/
      dotnet publish -c Debug -r ubuntu.16.10-x64
      
      # Extract CFGs from binary files.
      cd bin/Debug
      dotnet {directory_to_infersharp}/Cilsil/bin/Debug/netcoreapp2.2/Cilsil.dll translate \
                                                      netcoreapp2.2 \
                                                      --outcfg netcoreapp2.2/cfg.json \
                                                      --outtenv netcoreapp2.2/tenv.json \
                                                      --cfgtxt netcoreapp2.2/cfg.txt

      # Run infer backend analysis
      infer capture
      mkdir infer-out/captured
      infer analyzejson --debug \
                        --cfg-json netcoreapp2.2/cfg.json \
                        --tenv-json netcoreapp2.2/tenv.json
   ```