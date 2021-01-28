This directory contains a small example to play with Infer#. It includes some simple programming errors 
that is caught by Infer#.

Try this example by running
   ```bash
      # Build csharp_hello
      dotnet publish -c Debug -r ubuntu.16.10-x64
      
      # Extract CFGs from binary files.
      cd Examples/bin/Debug
      dotnet {directory_to_infersharp}/Cilsil/bin/Debug/net5.0/Cilsil.dll translate \
                                                      net5.0 \
                                                      --outcfg net5.0/cfg.json \
                                                      --outtenv net5.0/tenv.json \
                                                      --cfgtxt net5.0/cfg.txt

      # Run infer backend analysis
      infer capture
      mkdir infer-out/captured
      infer analyzejson --debug \
                        --cfg-json net5.0/cfg.json \
                        --tenv-json net5.0/tenv.json
   ```