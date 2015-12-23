%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%

%   CSEE 6893: Big Data Analytics
%   Bridge Health Monitoring
%   Author: Karl S. Bayer (karlsbayer at gmail dot com)
%   Created: 12/2015
  
%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%%
 
%% [f,xdft]=fftDataMag(time,data) 
% fftDataMag provides the frequency and magnitude of the a time series in
% terms of the peak-to-peak values of the input units 
% e.g. input is a time based voltage fft will produce a spectral plot that
% is in units of peak-to-peak values of the time based series ie Volts

%

function [freq,xdft]=my_fftDataMag(time,data,dim)

%Fs = 10;  % Sampling frequency
Fs=1./(time(2)-time(1));  %Samples per sec
t = time;
y = data;

NFFT = 2^(nextpow2(length(y))); % Next power of 2 from length of y Extend 
% to improve resolution of fft - changes bin size 
% HIgher number of bins provides for more accurate amplitude estimation of
% fft 

xdft = fft(y,NFFT,dim);
xdft = xdft(:, 1:length(xdft)/2+1);
xdft = 2*xdft/length(y);  %fixes magnitude such that it should match the input
%xdft(2:end-1) = 2*xdft(2:end-1);

%freq = Fs/2*linspace(0,1,NFFT/2+1);
freq = 0:Fs/(2*length(xdft)):Fs/2;
freq=freq(2:end);
% 
% % xdft = fft(x,2000);
% % xdft = xdft(1:length(xdft)/2+1);
% % xdft = xdft/length(x);
% % xdft(2:end-1) = 2*xdft(2:end-1);
% % freq = 0:Fs/(2*length(x)):Fs/2;
% 
% figure;
% subplot(211);
% plot(t,y)
% xlabel('Time [s]');
% ylabel('Z Translation [mm]');
% 
% subplot(212)
% semilogx(freq,abs(xdft));
% xlabel('Frequency (Hz)')
% ylabel('Peak Amplitude [mm]')
% 
% % subplot(313)
% % semilogx(freq,abs(xdft)/sqrt(2));
% % xlabel('Frequency (Hz)')
% % ylabel('Amplitude RMS [Units of Data]')
% 
% % NOTE Peak amplitude, a, is a single peak from the DC value or from 0 NOT to
% % be confused w/ peak-to-peak amplitude (2*a)
% 
% % To display sound pressure level in terms of dB 
% % SPL=20*log10(P'rms./20e-6Pa); 
% % Replot in SPL 
% figure;
% subplot(211);
% plot(t,y)
% xlabel('Time [s]');
% ylabel('Output [Pa]');
% 
% subplot(212)
% semilogx(freq,20*log10((abs(xdft)/sqrt(2))./20e-6))
% xlabel('Frequency (Hz)')
% ylabel('Sound Pressure Level [dB SPL]')
% %NOTE 20e-6 is in Pa and this is the human threshold for hearing 
