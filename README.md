# Molecular Construction Kit
This molecular construction kit allows the user to build simple molecules consisting of carbon and hydrogen atoms. Different atoms will follow in the future. The program is used with the HTC VIVE and the corresponding controllers. Created molecules can be saved and exported as an XML file to use them again.

The basic program was once created for use with the SenseGlove and part of my Bachelor Thesis, future updates will only support the VIVE Controllers and can be seen here: [MolecularVR](https://github.com/PatrickGe/MolecularVR).

## Controls
### Laserpointer
To activate the laserpointer, touch the front button on the touchpad of your VIVE Controller, now you are able to aim on your target. To confirm your selection, press the button completely. This is used for example to create new atoms. Therefore the user has to aim on the carbon plate on his right side before confirming his selection.

### Grab Atoms
To grab an atom, hold your VIVE Controller near it and press the Trigger. Release it, to let the atom go.

### Connect Atoms
To connect two atoms, grab one and drag it near the other. As soon as the minimal distance is passed, to atom it will be linked with is colored green. Now just let the grabbed atom go and they will link automatically.

### Edit Mode
To activate the edit mode, do the laserpointer gesture on one existing atom in the molecule. This atom will be marked red and is now fixed in space. Additional information, like the binding angles to the connected atoms, are now shown. Now several parts of the molecule can be grabbed and moved around. Intermolecular forces will put the atoms back into place as soon as the user doesn't grab them anymore.

### Delete Atoms
To delete the whole molecule, simply do the laserpointer gesture on the recycle bin. If the user only wants to delete a single atom, mark this one red by using the edit mode and do the laserpointer gesture on the recycle bin afterwards.

### Save and Load Molecules
To save created molecules as XML file, do the laserpointer gesture on the save plate in front of you. A virtual keyboard will pop up, where the user can enter the name. This also works by using the laserpointer. If the user wants to load and reuse an already created molecule, he can do the same with the load plate and a simple GUI with all created molecules will pop up.


