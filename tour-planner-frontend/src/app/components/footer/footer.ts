import { Component, inject} from '@angular/core';
import { ZardDialogService} from '@/shared/components/dialog';
import { Impressum } from '@/components/impressum/impressum';

@Component({
  selector: 'app-footer',
  imports: [],
  templateUrl: './footer.html',
  styleUrl: './footer.css',
})
export class Footer {
  private readonly dialog = inject(ZardDialogService);

  public openImpressum(): void {
    this.dialog.create({
      zContent: Impressum,
      zHideFooter: true,
      zMaskClosable: true,
      zClosable: false,
      zWidth: '50vw',
      zCustomClasses: 'max-h-[65vh] overflow-y-auto'
    });
  }
}
