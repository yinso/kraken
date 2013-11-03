kraken
======

Kraken is a Content Addressable Storage for .Net and Mono.

Prerequisite
========

* .Net runtime: version 4.5
* Nuget - you'll need it for pulling down the external dependencies or
  you'll have compilation errors.
* .Net IDE

Usage
======

Kraken is a command-line based tool today. 

### Saving Files Into Repo

    $ kraken save <local_path> <repo_path> 

Saves the `local_file` into the `repo_path`. If `local_path` is a
directory, it'll recursively save the files within the directory
(NOTE: if `repo_path` points to an existing file instead of directory,
it'll error out).

### Restore Files from Repo

    $ kraken restore <repo_path> <local_path>
    
Grab the file(s) at `repo_path` from kraken and restore to
`local_path`. 

### List Files In Repo

    $ kraken ls <repo_path> <depth_level> 

List the files stored in repo. `repo_path` defaults to `/`, and
`depth_level` defaults to `0` (current level only). If you want to recursive all the way
through - use `infinity` for `depth_level`.

### Grab Raw File from Repo

    $ kraken raw <repo_path>
    
Pump the raw content from `repo_path` to `STDOUT` - you can redirect
it to a file.

 

