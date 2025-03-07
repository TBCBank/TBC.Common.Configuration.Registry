// Copyright (C) TBC Bank. All Rights Reserved.

/*
 * Pin assembly version to 3.0.0.0 because it is a part of the strong name;
 * to avoid assembly load exceptions and the need for manual binding redirects,
 * just pin it to a static version, even if the file version keeps on incrementing.
 * Only bump it when major version changes.
 */
[assembly: System.Reflection.AssemblyVersion("3.0.0.0")]
